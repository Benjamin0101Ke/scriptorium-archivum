-- Integración 23/11/2026: historial docente existente y ejemplares por enlace.
BEGIN;
DO $migracion$
BEGIN
 IF to_regclass('public.revisiones_docentes') IS NOT NULL THEN
  INSERT INTO public.revisiones(id_entrega,id_usuario_revisor,fecha_inicio,fecha_cierre,ambito_revision,resultado,motivo_decision)
  SELECT d.id_entrega,d.id_docente,d.creado_en,d.creado_en,'Revisión docente',d.resultado,d.observaciones
  FROM public.revisiones_docentes d
  WHERE NOT EXISTS(SELECT 1 FROM public.revisiones r WHERE r.id_entrega=d.id_entrega
    AND r.id_usuario_revisor=d.id_docente AND r.ambito_revision='Revisión docente'
    AND r.fecha_cierre=d.creado_en AND r.resultado=d.resultado AND r.motivo_decision=d.observaciones);
 END IF;
END;
$migracion$;
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
 THEN
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

COMMIT;
