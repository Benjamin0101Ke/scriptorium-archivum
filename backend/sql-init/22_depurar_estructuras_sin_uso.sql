-- Depuración del esquema activo. Ejecutar con psql -v ON_ERROR_STOP=1.
-- Antes de aplicarlo a una base existente, guardar un pg_dump completo.
-- Conserva los valores retirados en un esquema privado de respaldo.
-- No modifica tablas reservadas ni elimina cuentas, trabajos o entregas.
BEGIN;
SET LOCAL lock_timeout = '10s';
CREATE SCHEMA IF NOT EXISTS respaldo_depuracion;
REVOKE ALL ON SCHEMA respaldo_depuracion FROM PUBLIC;
CREATE TABLE IF NOT EXISTS respaldo_depuracion.datos_20261128 (
    objeto text PRIMARY KEY,
    filas jsonb NOT NULL,
    creado_en timestamptz NOT NULL DEFAULT now()
);
REVOKE ALL ON respaldo_depuracion.datos_20261128 FROM PUBLIC;

DO $depuracion$
DECLARE
    tabla text;
    columnas text[];
    item record;
    contenido jsonb;
BEGIN
    -- No se utiliza CASCADE: una dependencia inesperada aborta la transacción.
    FOREACH tabla IN ARRAY ARRAY['palabras_clave_trabajo', 'permisos_trabajo',
                                 'historial_estados', 'evidencias_academicas'] LOOP
        IF to_regclass(format('public.%I', tabla)) IS NOT NULL THEN
            EXECUTE format('LOCK TABLE public.%I IN ACCESS EXCLUSIVE MODE', tabla);
            EXECUTE format('SELECT coalesce(jsonb_agg(to_jsonb(t)), ''[]''::jsonb) FROM public.%I t', tabla) INTO contenido;
            INSERT INTO respaldo_depuracion.datos_20261128(objeto, filas)
                VALUES ('public.' || tabla, contenido);
            EXECUTE format('DROP TABLE public.%I RESTRICT', tabla);
        END IF;
    END LOOP;

    FOR item IN SELECT * FROM (VALUES
        ('areas_tematicas', 'id_area', ARRAY['descripcion']),
        ('roles', 'id_rol', ARRAY['descripcion']),
        ('tipos_trabajo', 'id_tipo_trabajo', ARRAY['requiere_aprobacion_academica']),
        ('entregas', 'id_entrega', ARRAY['fecha_envio', 'nota_cambios']),
        ('trabajos', 'id_trabajo', ARRAY['objetivos', 'resultados', 'nivel_acceso', 'fecha_habilitacion'])
    ) AS cambios(tabla, clave, campos) LOOP
        SELECT array_agg(c.column_name::text ORDER BY c.ordinal_position) INTO columnas
          FROM information_schema.columns c
         WHERE c.table_schema = 'public' AND c.table_name = item.tabla
           AND c.column_name = ANY(item.campos);
        IF columnas IS NULL THEN CONTINUE; END IF;
        EXECUTE format('LOCK TABLE public.%I IN ACCESS EXCLUSIVE MODE', item.tabla);
        EXECUTE format(
            'SELECT coalesce(jsonb_agg(jsonb_build_object(%L, t.%I) || '
            || '(SELECT coalesce(jsonb_object_agg(k,v), ''{}''::jsonb) FROM jsonb_each(to_jsonb(t)) e(k,v) WHERE k = ANY($1))), ''[]''::jsonb) FROM public.%I t',
            item.clave, item.clave, item.tabla) INTO contenido USING columnas;
        INSERT INTO respaldo_depuracion.datos_20261128(objeto, filas)
            VALUES ('public.' || item.tabla || '.columnas', contenido);
        FOREACH tabla IN ARRAY columnas LOOP
            EXECUTE format('ALTER TABLE public.%I DROP COLUMN %I RESTRICT', item.tabla, tabla);
        END LOOP;
    END LOOP;
END;
$depuracion$;
COMMIT;
