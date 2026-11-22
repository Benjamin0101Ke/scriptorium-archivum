BEGIN;
SET LOCAL client_encoding = 'UTF8';

CREATE OR REPLACE FUNCTION public.fn_es_biblioteca(p_id_usuario integer)
RETURNS boolean
LANGUAGE sql
STABLE
SET search_path = pg_catalog, public
AS $funcion$
    SELECT EXISTS (
        SELECT 1
        FROM public.usuarios AS u
        JOIN public.roles AS r ON r.id_rol = u.id_rol
        WHERE u.id_usuario = p_id_usuario
          AND u.estado = 'Habilitada'
          AND r.activo
          AND r.nombre = 'Biblioteca'
    );
$funcion$;

-- La API comprueba el archivo y el canal antes de enviar true
-- Para una consulta publica, exigir tambien que la ficha tenga estado Publicado
CREATE OR REPLACE FUNCTION public.fn_elegibilidad_difusion(
    p_id_trabajo integer,
    p_canal_pdf_controlado boolean DEFAULT false
)
RETURNS TABLE (
    ficha_publica boolean,
    lectura boolean,
    descarga boolean,
    bloqueos text[]
)
LANGUAGE plpgsql
VOLATILE
SET search_path = pg_catalog, public
AS $funcion$
DECLARE
    ficha public.trabajos%ROWTYPE;
    ejemplar public.entregas%ROWTYPE;
    recurso public.recursos_digitales%ROWTYPE;
    constancia public.autorizaciones_difusion%ROWTYPE;
    decision public.revisiones%ROWTYPE;
    momento_revision timestamptz := clock_timestamp();
    ultima_suspension timestamptz;
    autorizacion_vigente boolean;
BEGIN
    ficha_publica := false;
    lectura := false;
    descarga := false;
    bloqueos := ARRAY[]::text[];

    SELECT t.* INTO ficha
    FROM public.trabajos AS t
    WHERE t.id_trabajo = p_id_trabajo;

    IF NOT FOUND THEN
        bloqueos := array_append(bloqueos, 'FICHA_INEXISTENTE');
        RETURN NEXT;
        RETURN;
    END IF;

    SELECT e.* INTO ejemplar
    FROM public.entregas AS e
    WHERE e.id_trabajo = ficha.id_trabajo
      AND e.id_entrega = ficha.id_entrega_final;

    SELECT r.* INTO recurso
    FROM public.recursos_digitales AS r
    WHERE r.id_trabajo = ficha.id_trabajo
      AND r.id_entrega = ejemplar.id_entrega
     AND r.clase IN ('PDF', 'REF_UES', 'ENLACE');

    SELECT a.* INTO constancia
    FROM public.autorizaciones_difusion AS a
    WHERE a.id_entrega = ejemplar.id_entrega
      AND a.estado = 'Activa';

    SELECT r.* INTO decision
    FROM public.revisiones AS r
    WHERE r.id_entrega = ejemplar.id_entrega
      AND r.ambito_revision = 'Difusión'
      AND r.fecha_cierre IS NOT NULL
    ORDER BY r.fecha_cierre DESC, r.id_revision DESC
    LIMIT 1;

    SELECT max(b.fecha_hora) INTO ultima_suspension
    FROM public.bitacora AS b
    WHERE b.tabla_afectada = 'trabajos'
      AND b.registro_afectado = ficha.id_trabajo::text
      AND b.operacion = 'SUSPENDER_DIFUSION';

    autorizacion_vigente := constancia.id_autorizacion IS NOT NULL
        AND constancia.fecha_otorgamiento <= momento_revision
        AND (
            constancia.vigencia_hasta IS NULL
            OR constancia.vigencia_hasta >= (momento_revision AT TIME ZONE 'America/El_Salvador')::date
        )
        AND EXISTS (
            SELECT 1
            FROM public.personas_autorizaciones AS otorgante
            WHERE otorgante.id_autorizacion = constancia.id_autorizacion
        );

    IF ficha.titulo !~ '[^[:space:]]' OR ficha.resumen !~ '[^[:space:]]'
       OR ficha.anio NOT BETWEEN 1000 AND 9999
       OR ficha.id_carrera IS NULL OR ficha.id_tipo_trabajo IS NULL
       OR NOT EXISTS (
           SELECT 1 FROM public.participaciones AS p
           WHERE p.id_trabajo = ficha.id_trabajo AND p.calidad_participacion = 'Autor'
       )
       OR NOT EXISTS (
           SELECT 1 FROM public.trabajos_areas AS a WHERE a.id_trabajo = ficha.id_trabajo
       ) THEN
        bloqueos := array_append(bloqueos, '01_METADATOS_INCOMPLETOS');
    END IF;

    IF ficha.referencia_deposito_ues IS NULL
       OR ficha.deposito_verificado_por IS NULL OR ficha.deposito_verificado_en IS NULL
       OR ficha.deposito_verificado_en > momento_revision THEN
        bloqueos := array_append(bloqueos, '02_DEPOSITO_NO_VERIFICADO');
    END IF;

    IF ejemplar.estado IS DISTINCT FROM 'Verificado'
       OR ejemplar.referencia_conformidad IS NULL OR ejemplar.verificado_por IS NULL
       OR ejemplar.verificado_en IS NULL OR ejemplar.verificado_en > momento_revision
       OR recurso.id_recurso IS NULL THEN
        bloqueos := array_append(bloqueos, '03_EJEMPLAR_NO_VERIFICADO');
    END IF;

    IF autorizacion_vigente IS NOT TRUE THEN
        bloqueos := array_append(bloqueos, '04_CONSTANCIA_NO_VIGENTE');
    END IF;

    IF decision.resultado IS DISTINCT FROM 'Favorable'
       OR decision.fecha_cierre > momento_revision
       OR (ultima_suspension IS NOT NULL AND decision.fecha_cierre <= ultima_suspension) THEN
        bloqueos := array_append(bloqueos, '05_DECISION_NO_FAVORABLE_O_PENDIENTE');
    END IF;

    IF ejemplar.difundir_ficha_habilitado IS NOT TRUE
       OR constancia.difundir_ficha_concedido IS NOT TRUE THEN
        bloqueos := array_append(bloqueos, '06_FICHA_NO_AUTORIZADA');
    END IF;

    IF (ejemplar.leer_pdf_habilitado AND constancia.leer_pdf_concedido IS NOT TRUE)
       OR (ejemplar.descargar_pdf_habilitado AND constancia.descargar_pdf_concedido IS NOT TRUE) THEN
        bloqueos := array_append(bloqueos, '07_PERMISOS_PDF_SIN_RESPALDO');
    END IF;

    IF ejemplar.leer_pdf_habilitado OR ejemplar.descargar_pdf_habilitado THEN
        IF p_canal_pdf_controlado IS NOT TRUE
           OR (recurso.clase IS DISTINCT FROM 'PDF' AND recurso.clase IS DISTINCT FROM 'REF_UES')
           OR (recurso.clase = 'PDF' AND recurso.estado_carga IS DISTINCT FROM 'Disponible') THEN
            bloqueos := array_append(bloqueos, '08_PDF_NO_DISPONIBLE_O_NO_CONTROLADO');
        END IF;
    END IF;

    IF cardinality(bloqueos) = 0 THEN
        ficha_publica := true;
        lectura := ejemplar.leer_pdf_habilitado;
        descarga := ejemplar.descargar_pdf_habilitado;
    END IF;

    RETURN NEXT;
END;
$funcion$;

-- Las transiciones se registran una sola vez en bitacora
CREATE OR REPLACE FUNCTION public.fn_registrar_bitacora_estado()
RETURNS trigger
LANGUAGE plpgsql
SET search_path = pg_catalog, public
AS $funcion$
DECLARE
    actor_id integer := NULLIF(current_setting('scriptorium.actor_id', true), '')::integer;
    razon text := NULLIF(current_setting('scriptorium.motivo_estado', true), '');
    evento text := 'CAMBIAR_ESTADO_PUBLICACION';
BEGIN
    IF NEW.estado_publicacion = 'Retirado' THEN
        evento := 'RETIRAR_TRABAJO';
        actor_id := NEW.id_usuario_retira;
        razon := NEW.motivo_retiro;
    ELSIF NEW.estado_publicacion = 'Publicado' THEN
        evento := 'PUBLICAR_TRABAJO';
    ELSIF OLD.estado_publicacion = 'Publicado' AND NEW.estado_publicacion = 'En revisión' THEN
        evento := 'SUSPENDER_DIFUSION';
    END IF;

    INSERT INTO public.bitacora (
        id_usuario_actor, actor_tecnico, operacion, tabla_afectada,
        registro_afectado, fecha_hora, detalle
    )
    VALUES (
        actor_id,
        CASE WHEN actor_id IS NULL THEN session_user::text END,
        evento,
        'trabajos',
        NEW.id_trabajo::text,
        clock_timestamp(),
        format('%s -> %s', OLD.estado_publicacion, NEW.estado_publicacion)
            || CASE WHEN razon IS NOT NULL THEN ' | ' || razon ELSE '' END
    );

    RETURN NEW;
END;
$funcion$;

DROP TRIGGER IF EXISTS trg_bitacora_estado_publicacion ON public.trabajos;
CREATE TRIGGER trg_bitacora_estado_publicacion
AFTER UPDATE OF estado_publicacion ON public.trabajos
FOR EACH ROW
WHEN (OLD.estado_publicacion IS DISTINCT FROM NEW.estado_publicacion)
EXECUTE FUNCTION public.fn_registrar_bitacora_estado();

CREATE OR REPLACE PROCEDURE public.sp_publicar_trabajo(
    IN p_id_trabajo integer,
    IN p_id_usuario integer,
    IN p_canal_pdf_controlado boolean,
    INOUT p_bloqueos text[] DEFAULT NULL
)
LANGUAGE plpgsql
SET search_path = pg_catalog, public
AS $procedimiento$
DECLARE
    estado_actual text;
    actor_previo text := current_setting('scriptorium.actor_id', true);
BEGIN
    IF NOT public.fn_es_biblioteca(p_id_usuario) THEN
        p_bloqueos := ARRAY['09_ACTOR_NO_AUTORIZADO'];
        RETURN;
    END IF;

    SELECT t.estado_publicacion INTO estado_actual
    FROM public.trabajos AS t WHERE t.id_trabajo = p_id_trabajo
    FOR UPDATE;

    IF NOT FOUND THEN
        p_bloqueos := ARRAY['FICHA_INEXISTENTE'];
        RETURN;
    END IF;

    SELECT e.bloqueos INTO p_bloqueos
    FROM public.fn_elegibilidad_difusion(p_id_trabajo, p_canal_pdf_controlado) AS e;

    IF estado_actual NOT IN ('Borrador', 'En revisión') THEN
        p_bloqueos := array_append(p_bloqueos, '09_ESTADO_ORIGEN_INVALIDO');
    END IF;

    IF cardinality(p_bloqueos) > 0 THEN
        RETURN;
    END IF;

    PERFORM set_config('scriptorium.actor_id', p_id_usuario::text, true);
    UPDATE public.trabajos
    SET estado_publicacion = 'Publicado', fecha_publicacion = clock_timestamp()
    WHERE id_trabajo = p_id_trabajo;
    PERFORM set_config('scriptorium.actor_id', coalesce(actor_previo, ''), true);
END;
$procedimiento$;

CREATE OR REPLACE PROCEDURE public.sp_retirar_trabajo(
    IN p_id_trabajo integer,
    IN p_id_usuario integer,
    IN p_motivo text
)
LANGUAGE plpgsql
SET search_path = pg_catalog, public
AS $procedimiento$
BEGIN
    IF NOT public.fn_es_biblioteca(p_id_usuario) THEN
        RAISE EXCEPTION 'Se requiere una cuenta habilitada de Biblioteca'
            USING ERRCODE = '42501';
    END IF;
    IF p_motivo IS NULL OR p_motivo !~ '[^[:space:]]' THEN
        RAISE EXCEPTION 'El retiro requiere un motivo'
            USING ERRCODE = '22023';
    END IF;

    UPDATE public.trabajos
    SET estado_publicacion = 'Retirado',
        fecha_retiro = clock_timestamp(),
        motivo_retiro = btrim(p_motivo),
        id_usuario_retira = p_id_usuario
    WHERE id_trabajo = p_id_trabajo AND estado_publicacion = 'Publicado';

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Solo se puede retirar una ficha publicada'
            USING ERRCODE = '23514';
    END IF;
END;
$procedimiento$;

-- Invocar en la transaccion que sustituye el ejemplar o invalida la constancia
CREATE OR REPLACE PROCEDURE public.sp_suspender_difusion(
    IN p_id_trabajo integer,
    IN p_id_usuario integer,
    IN p_motivo text
)
LANGUAGE plpgsql
SET search_path = pg_catalog, public
AS $procedimiento$
DECLARE
    actor_previo text := current_setting('scriptorium.actor_id', true);
    motivo_previo text := current_setting('scriptorium.motivo_estado', true);
BEGIN
    IF NOT public.fn_es_biblioteca(p_id_usuario) THEN
        RAISE EXCEPTION 'Se requiere una cuenta habilitada de Biblioteca'
            USING ERRCODE = '42501';
    END IF;
    IF p_motivo IS NULL OR p_motivo !~ '[^[:space:]]' THEN
        RAISE EXCEPTION 'La suspension requiere un motivo'
            USING ERRCODE = '22023';
    END IF;

    PERFORM set_config('scriptorium.actor_id', p_id_usuario::text, true);
    PERFORM set_config('scriptorium.motivo_estado', btrim(p_motivo), true);

    UPDATE public.trabajos
    SET estado_publicacion = 'En revisión'
    WHERE id_trabajo = p_id_trabajo AND estado_publicacion = 'Publicado';

    PERFORM set_config('scriptorium.actor_id', coalesce(actor_previo, ''), true);
    PERFORM set_config('scriptorium.motivo_estado', coalesce(motivo_previo, ''), true);
END;
$procedimiento$;

COMMIT;
