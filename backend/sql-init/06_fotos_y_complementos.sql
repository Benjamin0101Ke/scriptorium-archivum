\connect scriptorium_archivum

ALTER TABLE public.usuarios
    ADD COLUMN IF NOT EXISTS foto_perfil bytea,
    ADD COLUMN IF NOT EXISTS foto_tipo varchar(30),
    ADD COLUMN IF NOT EXISTS foto_version uuid;

CREATE TABLE IF NOT EXISTS public.archivos_complementarios (
    id_archivo bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    id_trabajo integer NOT NULL REFERENCES public.trabajos(id_trabajo),
    id_usuario integer NOT NULL REFERENCES public.usuarios(id_usuario),
    id_entrega integer REFERENCES public.entregas(id_entrega),
    nombre varchar(255) NOT NULL,
    clave varchar(50) NOT NULL UNIQUE,
    descripcion varchar(1500) NOT NULL,
    tamano_bytes bigint NOT NULL CHECK (tamano_bytes > 0 AND tamano_bytes <= 26214400),
    habilitado boolean NOT NULL DEFAULT false,
    fecha_registro timestamptz NOT NULL DEFAULT now()
);
