\connect scriptorium_archivum
BEGIN;

DO $departamentos$
BEGIN
    IF to_regclass('public.areas_academicas') IS NOT NULL THEN
        IF to_regclass('public.departamentos') IS NOT NULL THEN
            RAISE EXCEPTION 'Existen ambos catálogos. Revisá sus datos antes de unificarlos.';
        END IF;
        ALTER TABLE public.areas_academicas RENAME TO departamentos;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns
        WHERE table_schema='public' AND table_name='departamentos' AND column_name='id_area_academica') THEN
        ALTER TABLE public.departamentos RENAME COLUMN id_area_academica TO id_departamento;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns
        WHERE table_schema='public' AND table_name='carreras' AND column_name='id_area_academica') THEN
        ALTER TABLE public.carreras RENAME COLUMN id_area_academica TO id_departamento;
    END IF;
END;
$departamentos$;

COMMIT;
