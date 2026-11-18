BEGIN;

INSERT INTO public.roles (nombre) VALUES
    ('Lector'), ('Estudiante'), ('Biblioteca'), ('Docente revisor'), ('Administrador')
ON CONFLICT (nombre) DO NOTHING;

ALTER TABLE public.usuarios ADD COLUMN IF NOT EXISTS version_rol integer NOT NULL DEFAULT 0;

CREATE TABLE IF NOT EXISTS public.recuperaciones_contrasena (
    id_usuario integer PRIMARY KEY REFERENCES public.usuarios(id_usuario) ON DELETE CASCADE,
    codigo_protegido text,
    vence_en timestamptz NOT NULL,
    ultimo_envio timestamptz NOT NULL,
    inicio_ventana timestamptz NOT NULL,
    envios_ventana integer NOT NULL DEFAULT 1,
    intentos integer NOT NULL DEFAULT 0
);

COMMIT;
