BEGIN;

-- Conserva los favoritos existentes y descarta las puntuaciones numericas
DROP TABLE IF EXISTS public.valoraciones;

-- Cuenta las marcas activas de cada trabajo
CREATE OR REPLACE VIEW public.vw_estrellas_por_trabajo AS
SELECT
    id_trabajo,
    count(*) AS estrellas
FROM public.favoritos
WHERE activo
GROUP BY id_trabajo;

COMMIT;
