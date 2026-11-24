BEGIN;

ALTER TABLE public.archivos_complementarios
    DROP CONSTRAINT IF EXISTS archivos_complementarios_tamano_bytes_check;
ALTER TABLE public.archivos_complementarios
    DROP CONSTRAINT IF EXISTS ck_archivos_complementarios_tamano;
ALTER TABLE public.archivos_complementarios
    ADD CONSTRAINT ck_archivos_complementarios_tamano
        CHECK (tamano_bytes > 0 AND tamano_bytes <= 6442450944);

COMMIT;
