-- Cargar manualmente en una base de practica vacia
BEGIN;
SET LOCAL client_encoding = 'UTF8';

DO $practica$
BEGIN
    IF EXISTS (SELECT 1 FROM public.usuarios) OR EXISTS (SELECT 1 FROM public.trabajos) THEN
        RAISE EXCEPTION 'Los datos de practica requieren una base sin cuentas ni fichas';
    END IF;
END;
$practica$;

INSERT INTO public.roles(nombre) VALUES ('Biblioteca'), ('Lector')
ON CONFLICT (nombre) DO NOTHING;

INSERT INTO public.carreras(codigo_carrera,nombre)
VALUES ('SIS','Ingeniería en Sistemas Informáticos') ON CONFLICT DO NOTHING;

INSERT INTO public.tipos_trabajo(nombre)
VALUES ('Trabajo de grado') ON CONFLICT DO NOTHING;

INSERT INTO public.areas_tematicas(nombre)
VALUES ('Gestión documental') ON CONFLICT DO NOTHING;

WITH personas_prueba AS (
    INSERT INTO public.personas(nombres,apellidos)
    VALUES ('Biblioteca','de Prueba'), ('Lectura','de Prueba'), ('Ana María','Peña de Prueba')
    RETURNING id_persona, nombres
)
INSERT INTO public.usuarios(id_persona,correo_institucional,hash_contrasena,id_rol)
SELECT p.id_persona, 'biblioteca@scriptorium.test', 'AQAAAAIAAYagAAAAEF3NwofNDSCVRqtQjbP9jBCLa5cYsfaSPg6hACyHplb4Y2TF2zBsXFRuEslF4WJCYQ==', r.id_rol
FROM personas_prueba p CROSS JOIN public.roles r
WHERE p.nombres='Biblioteca' AND r.nombre='Biblioteca'
UNION ALL
SELECT p.id_persona, 'lector@scriptorium.test', 'AQAAAAIAAYagAAAAEKYsyM2BAC2fc4N6ThRRDPpyLyKVFjBAp/BjZ98/eRcaLywEeH/WVSU2Sn1IKzn6tg==', r.id_rol
FROM personas_prueba p CROSS JOIN public.roles r
WHERE p.nombres='Lectura' AND r.nombre='Lector';

WITH metadatos AS (
    SELECT c.id_carrera, t.id_tipo_trabajo
    FROM public.carreras c CROSS JOIN public.tipos_trabajo t
    WHERE c.nombre='Ingeniería en Sistemas Informáticos' AND t.nombre='Trabajo de grado'
), fichas_prueba AS (
    INSERT INTO public.trabajos(codigo,titulo,anio,resumen,id_carrera,id_tipo_trabajo,estado_publicacion)
    SELECT x.codigo,x.titulo,2025,x.resumen,m.id_carrera,m.id_tipo_trabajo,x.estado
    FROM metadatos m
    CROSS JOIN (VALUES
        ('QA-BORRADOR','Gestión de información','Ficha para probar carga y publicación.','Borrador'),
        ('QA-REVISION','Archivo académico','Ficha para probar el bloqueo de publicación.','En revisión')
    ) AS x(codigo,titulo,resumen,estado)
    RETURNING id_trabajo
), vinculaciones AS (
    INSERT INTO public.participaciones(id_trabajo,id_persona,calidad_participacion,orden_autoria)
    SELECT f.id_trabajo,p.id_persona,'Autor',1
    FROM fichas_prueba f CROSS JOIN public.personas p
    WHERE p.nombres='Ana María' AND p.apellidos='Peña de Prueba'
)
INSERT INTO public.trabajos_areas(id_trabajo,id_area)
SELECT f.id_trabajo,a.id_area
FROM fichas_prueba f CROSS JOIN public.areas_tematicas a
WHERE a.nombre='Gestión documental';

COMMIT;
