\connect scriptorium_archivum
BEGIN;

CREATE TABLE IF NOT EXISTS public.valoraciones (
    id_usuario integer NOT NULL REFERENCES public.usuarios(id_usuario),
    id_trabajo integer NOT NULL REFERENCES public.trabajos(id_trabajo),
    estrellas smallint NOT NULL CHECK (estrellas BETWEEN 1 AND 5),
    fecha_registro timestamptz NOT NULL DEFAULT now(),
    fecha_actualizacion timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (id_usuario,id_trabajo)
);
CREATE INDEX IF NOT EXISTS ix_valoraciones_trabajo ON public.valoraciones(id_trabajo);

COMMIT;
