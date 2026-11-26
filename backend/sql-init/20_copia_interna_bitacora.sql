BEGIN;

-- Configure the institutional destination privately in each installation.
-- No retroactive mailing: only new, committed audit events are copied.
CREATE TABLE IF NOT EXISTS public.configuracion_correo_bitacora (
    unica boolean PRIMARY KEY DEFAULT true CHECK (unica),
    destino varchar(254),
    habilitado boolean NOT NULL DEFAULT false,
    CHECK (NOT habilitado OR (destino IS NOT NULL AND destino ~ '^[^[:space:]@]+@[^[:space:]@]+$'))
);
INSERT INTO public.configuracion_correo_bitacora(unica) VALUES (true)
ON CONFLICT (unica) DO NOTHING;

CREATE OR REPLACE FUNCTION public.fn_encolar_copia_bitacora(p_evento integer)
RETURNS void
LANGUAGE plpgsql
SET search_path = pg_catalog, public
AS $funcion$
DECLARE
    destino_interno text;
    evento public.bitacora%ROWTYPE;
    responsable text;
    referencia text;
    destinatarios text;
    avisos text;
    titulo_correo text;
    cuerpo text;
BEGIN
    SELECT lower(btrim(destino)) INTO destino_interno
    FROM public.configuracion_correo_bitacora WHERE unica AND habilitado;
    IF destino_interno IS NULL THEN RETURN; END IF;

    SELECT * INTO evento FROM public.bitacora WHERE id_evento=p_evento;
    IF NOT FOUND THEN RETURN; END IF;
    SELECT concat_ws(' ', coalesce(u.nombre_mostrar, concat_ws(' ',p.nombres,p.apellidos)),
        '(' || u.correo_institucional || ')') INTO responsable
    FROM public.usuarios u JOIN public.personas p USING(id_persona)
    WHERE u.id_usuario=evento.id_usuario_actor;
    responsable := coalesce(responsable, evento.actor_tecnico, 'Sistema');

    IF evento.tabla_afectada='trabajos' THEN
        SELECT titulo INTO referencia FROM public.trabajos
        WHERE id_trabajo::text=evento.registro_afectado;
    ELSIF evento.tabla_afectada='usuarios' THEN
        SELECT concat_ws(' ',coalesce(u.nombre_mostrar,concat_ws(' ',p.nombres,p.apellidos)),
            '(' || u.correo_institucional || ')') INTO referencia
        FROM public.usuarios u JOIN public.personas p USING(id_persona)
        WHERE u.id_usuario::text=evento.registro_afectado;
    END IF;

    SELECT string_agg(DISTINCT m.destino, ', ' ORDER BY m.destino),
        string_agg(DISTINCT m.mensaje, E'\n\n---\n\n')
    INTO destinatarios, avisos
    FROM public.correos_eventos m
    WHERE m.id_evento=p_evento AND lower(m.destino)<>destino_interno;

    titulo_correo := left('Registro: ' || replace(evento.operacion,'_',' '), 150);
    cuerpo := format(E'Registro de actividad #%s\nAcción: %s\nResponsable: %s\nFecha: %s — El Salvador\nRegistro: %s #%s%s\n\nDetalle / motivo:\n%s\n\nDestinatarios de los avisos asociados: %s\n\n%s\n\nEsta copia confirma el registro de la acción. No confirma que los destinatarios hayan recibido o leído sus correos. El resultado de envío se consulta en el registro de correos.',
        evento.id_evento, replace(evento.operacion,'_',' '), responsable,
        to_char(evento.fecha_hora AT TIME ZONE 'America/El_Salvador','DD/MM/YYYY HH24:MI:SS'),
        evento.tabla_afectada, evento.registro_afectado,
        CASE WHEN referencia IS NULL THEN '' ELSE ' — ' || referencia END,
        coalesce(evento.detalle,'Sin detalle adicional.'),
        coalesce(destinatarios,'No se generaron avisos adicionales para este evento.'),
        coalesce(avisos,''));

    INSERT INTO public.correos_eventos(id_evento,destino,asunto,titulo,mensaje)
    VALUES(p_evento,destino_interno,titulo_correo || ' · Scriptorium Archivum',titulo_correo,cuerpo)
    ON CONFLICT(id_evento,destino) DO UPDATE
        SET asunto=EXCLUDED.asunto,titulo=EXCLUDED.titulo,mensaje=EXCLUDED.mensaje
        WHERE correos_eventos.estado='Pendiente' AND correos_eventos.intentos=0;
END;
$funcion$;

CREATE OR REPLACE FUNCTION public.fn_copiar_bitacora_por_correo()
RETURNS trigger
LANGUAGE plpgsql
SET search_path = pg_catalog, public
AS $funcion$
BEGIN
    PERFORM public.fn_encolar_copia_bitacora(NEW.id_evento);
    RETURN NEW;
END;
$funcion$;

-- Deferred until commit so recipients and reasons inserted by the API in the
-- same transaction are present. A rollback also discards the internal email.
DROP TRIGGER IF EXISTS trg_correo_copia_bitacora ON public.bitacora;
CREATE CONSTRAINT TRIGGER trg_correo_copia_bitacora
AFTER INSERT ON public.bitacora
DEFERRABLE INITIALLY DEFERRED
FOR EACH ROW EXECUTE FUNCTION public.fn_copiar_bitacora_por_correo();

COMMIT;
