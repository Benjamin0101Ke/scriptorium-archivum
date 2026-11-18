BEGIN;

ALTER TABLE public.usuarios
    ADD COLUMN IF NOT EXISTS nombre_mostrar varchar(120),
    ADD COLUMN IF NOT EXISTS avatar varchar(12) NOT NULL DEFAULT 'turquesa',
    ADD COLUMN IF NOT EXISTS tema varchar(10) NOT NULL DEFAULT 'sistema';

ALTER TABLE public.trabajos
    ADD COLUMN IF NOT EXISTS id_usuario_envio integer REFERENCES public.usuarios (id_usuario);

CREATE INDEX IF NOT EXISTS ix_trabajos_usuario_envio ON public.trabajos(id_usuario_envio)
    WHERE id_usuario_envio IS NOT NULL;

COMMIT;
