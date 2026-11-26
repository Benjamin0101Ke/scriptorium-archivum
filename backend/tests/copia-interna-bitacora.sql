-- Run after migration 20. Everything, including the test destination, rolls back.
\set ON_ERROR_STOP on
BEGIN;
UPDATE public.configuracion_correo_bitacora
SET destino='auditoria@scriptorium.test',habilitado=true WHERE unica;
DO $prueba$
DECLARE
    evento integer;
    mensaje text;
BEGIN
    INSERT INTO public.bitacora(actor_tecnico,operacion,tabla_afectada,registro_afectado,detalle)
    VALUES('Prueba automática','DECISION_BIBLIOTECA','trabajos','0','Desfavorable: se requiere aclaración.')
    RETURNING id_evento INTO evento;
    IF EXISTS(SELECT FROM public.correos_eventos WHERE id_evento=evento) THEN
        RAISE EXCEPTION 'La copia se adelantó al cierre de la transacción';
    END IF;
    INSERT INTO public.correos_eventos(id_evento,destino,asunto,titulo,mensaje)
    VALUES(evento,'alumno@scriptorium.test','Decisión','Decisión','Resultado: Desfavorable. Motivo: se requiere aclaración.');
    SET CONSTRAINTS trg_correo_copia_bitacora IMMEDIATE;
    SELECT m.mensaje INTO mensaje FROM public.correos_eventos m
    WHERE id_evento=evento AND destino='auditoria@scriptorium.test';
    IF mensaje IS NULL OR mensaje NOT LIKE '%alumno@scriptorium.test%'
       OR mensaje NOT LIKE '%Desfavorable%' OR mensaje NOT LIKE '%Prueba automática%' THEN
        RAISE EXCEPTION 'Faltan destinatario, motivo o responsable';
    END IF;
    PERFORM public.fn_encolar_copia_bitacora(evento);
    IF (SELECT count(*) FROM public.correos_eventos
        WHERE id_evento=evento AND destino='auditoria@scriptorium.test')<>1 THEN
        RAISE EXCEPTION 'Copia duplicada';
    END IF;
    RAISE NOTICE 'OK: copia diferida, motivo, responsable, destinatarios y deduplicación';

    SET CONSTRAINTS trg_correo_copia_bitacora DEFERRED;
    INSERT INTO public.bitacora(actor_tecnico,operacion,tabla_afectada,registro_afectado,detalle)
    VALUES('Prueba automática','CAMBIAR_ESTADO_CUENTA','usuarios','0','Deshabilitada')
    RETURNING id_evento INTO evento;
    -- An existing office notification must be enriched, not duplicated.
    INSERT INTO public.correos_eventos(id_evento,destino,asunto,titulo,mensaje)
    VALUES(evento,'auditoria@scriptorium.test','Previo','Previo','Previo');
    SET CONSTRAINTS trg_correo_copia_bitacora IMMEDIATE;
    IF (SELECT count(*) FROM public.correos_eventos WHERE id_evento=evento)<>1
       OR NOT EXISTS(SELECT FROM public.correos_eventos m WHERE id_evento=evento
          AND m.mensaje LIKE '%Deshabilitada%' AND m.mensaje LIKE '%No se generaron avisos adicionales%') THEN
        RAISE EXCEPTION 'La acción de cuenta no se copió correctamente';
    END IF;
    RAISE NOTICE 'OK: acciones sin destinatarios y copia institucional ya existente';

    UPDATE public.configuracion_correo_bitacora SET habilitado=false WHERE unica;
    INSERT INTO public.bitacora(actor_tecnico,operacion,tabla_afectada,registro_afectado)
    VALUES('Prueba automática','REGISTRO_CUENTA','usuarios','0') RETURNING id_evento INTO evento;
    IF EXISTS(SELECT FROM public.correos_eventos WHERE id_evento=evento) THEN
        RAISE EXCEPTION 'Se ignoró la configuración deshabilitada';
    END IF;
    RAISE NOTICE 'OK: configuración deshabilitada';
END;
$prueba$;
ROLLBACK;
