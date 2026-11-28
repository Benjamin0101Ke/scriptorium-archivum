-- Solo para el contenedor aislado creado por depuracion.ps1.
DO $prueba$
DECLARE nombre text;
BEGIN
    IF (SELECT count(*) FROM pg_tables WHERE schemaname='public') <> 30 THEN
        RAISE EXCEPTION 'Cantidad incorrecta de tablas activas';
    END IF;
    IF (SELECT count(*) FROM trabajos WHERE codigo='PRUEBA-DEPURACION') <> 1 THEN
        RAISE EXCEPTION 'Se perdió el trabajo operativo';
    END IF;
    FOREACH nombre IN ARRAY ARRAY['palabras_clave_trabajo','permisos_trabajo','historial_estados','evidencias_academicas'] LOOP
        IF to_regclass('public.' || nombre) IS NOT NULL THEN
            RAISE EXCEPTION 'No se retiró %', nombre;
        END IF;
        IF (SELECT jsonb_array_length(filas) FROM respaldo_depuracion.datos_20261128
              WHERE objeto='public.' || nombre) IS DISTINCT FROM 1 THEN
            RAISE EXCEPTION 'No se conservó el registro de %', nombre;
        END IF;
    END LOOP;
    IF (SELECT filas->0->>'objetivos' FROM respaldo_depuracion.datos_20261128
          WHERE objeto='public.trabajos.columnas') IS DISTINCT FROM 'Objetivo conservado'
       OR (SELECT filas->0->>'nota_cambios' FROM respaldo_depuracion.datos_20261128
          WHERE objeto='public.entregas.columnas') IS DISTINCT FROM 'Nota conservada' THEN
        RAISE EXCEPTION 'No se conservaron los valores retirados';
    END IF;
    FOREACH nombre IN ARRAY ARRAY['solicitudes','observaciones','distinciones_destacado','procedimientos_deposito','presencia_sesiones','revisiones_docentes'] LOOP
        IF to_regclass('public.' || nombre) IS NULL THEN
            RAISE EXCEPTION 'Se retiró indebidamente %', nombre;
        END IF;
    END LOOP;
END;
$prueba$;
