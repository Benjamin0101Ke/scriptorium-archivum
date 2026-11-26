BEGIN;

-- Facilita el conteo de estrellas por trabajo sin incluir marcas inactivas
CREATE INDEX IF NOT EXISTS favoritos_estrellas_activas
    ON public.favoritos(id_trabajo)
    WHERE activo;

COMMIT;
