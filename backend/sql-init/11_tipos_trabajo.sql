\connect scriptorium_archivum
BEGIN;

ALTER TABLE public.tipos_trabajo ADD COLUMN IF NOT EXISTS activa boolean NOT NULL DEFAULT true;

INSERT INTO public.tipos_trabajo(nombre) VALUES
    ('Trabajo de grado'),
    ('Tesis'),
    ('Proyecto de investigación'),
    ('Proyecto de software'),
    ('Informe técnico'),
    ('Monografía')
ON CONFLICT(nombre) DO NOTHING;

COMMIT;
