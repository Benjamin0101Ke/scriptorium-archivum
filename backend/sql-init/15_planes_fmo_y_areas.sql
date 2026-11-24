-- Fuente oficial: https://eel.ues.edu.sv/ingreso/index/contenido/679
-- Catálogo FMO: https://nextcloud.ues.edu.sv/s/B5Bjr44tRQXZNeD/download
-- Consulta: 2026-11-19. Versiones documentadas; no se afirma que sean las últimas vigentes.
-- No reemplaza planes creados por administración ni desactiva versiones existentes.
BEGIN;
CREATE TEMP TABLE planes_fmo_carga(nombre_carrera text,nombre_plan text,pagina integer) ON COMMIT DROP;
INSERT INTO planes_fmo_carga VALUES
('Doctorado en Medicina','Plan 1992',109),
('Licenciatura en Laboratorio Clínico','Plan 1997',116),
('Licenciatura en Fisioterapia','Plan 1997 (Fisioterapia y Terapia Ocupacional)',114),
('Arquitectura','Plan 1998',35),
('Ingeniería Civil','Plan 1998',28),
('Ingeniería Industrial','Plan 2017',24),
('Ingeniería Mecánica','Plan 1998',44),
('Ingeniería Eléctrica','Plan 1998',39),
('Ingeniería en Sistemas Informáticos','Plan 1998',32),
('Ingeniería Agronómica','Plan 2021',15),
('Licenciatura en Ciencias Jurídicas','Plan 2007',11),
('Licenciatura en Educación Inicial y Parvularia','Plan 2013',85),
('Licenciatura en Psicología','Plan 2022',82),
('Licenciatura en Sociología','Plan 2005',72),
('Licenciatura en Letras','Plan 2009',78),
('Licenciatura en Idiomas Francés e Inglés','Plan 2002 (Lenguas Modernas: Francés e Inglés)',75),
('Profesorado en Idioma Inglés para Tercer Ciclo de Educación Básica y Educación Media','Plan 2013',91),
('Profesorado en Ciencias Sociales para Tercer Ciclo de Educación Básica y Educación Media','Plan 2013',88),
('Profesorado en Matemática para Tercer Ciclo de Educación Básica y Educación Media','Plan 2013',62),
('Profesorado en Biología para Tercer Ciclo de Educación Básica y Educación Media','Plan 2013',66),
('Profesorado en Física para Tercer Ciclo de Educación Básica y Educación Media','Plan 2013',64),
('Profesorado en Química para Tercer Ciclo de Educación Básica y Educación Media','Plan 2013',68),
('Licenciatura en Química y Farmacia','Plan 2014',18),
('Licenciatura en Economía','Plan 1994',94),
('Licenciatura en Contaduría Pública','Plan 1994',102),
('Licenciatura en Administración de Empresas','Plan 1994',97),
('Licenciatura en Mercadeo Internacional','Plan 2004',100),
('Licenciatura en Logística Comercial Internacional (En línea)','Plan 2022',105),
('Licenciatura en Matemática','Plan 2003',50),
('Licenciatura en Ciencias Químicas','Plan 2003',59),
('Licenciatura en Física','Plan 2003',56),
('Licenciatura en Biología','Plan 2003',53);
INSERT INTO public.planes_estudio(id_carrera,nombre,documento_url,activa)
SELECT c.id_carrera,p.nombre_plan,'https://nextcloud.ues.edu.sv/s/B5Bjr44tRQXZNeD/download#page='||p.pagina,true
FROM planes_fmo_carga p JOIN public.carreras c ON c.nombre=p.nombre_carrera
ON CONFLICT(id_carrera,nombre) DO NOTHING;
INSERT INTO public.areas_tematicas(nombre,activa) VALUES
('Inteligencia artificial',true),('Desarrollo de software',true),('Ciberseguridad',true),
('Redes e Internet de las Cosas',true),('Bases de datos',true),('Videojuegos y simulación',true),
('Ingeniería y procesos industriales',true),('Energía y electricidad',true),('Construcción y estructuras',true),
('Arquitectura y urbanismo',true),('Salud pública',true),('Investigación clínica',true),
('Rehabilitación y fisioterapia',true),('Química y ciencias farmacéuticas',true),
('Agricultura sostenible',true),('Biodiversidad y medio ambiente',true),('Educación y didáctica',true),
('Psicología y bienestar',true),('Derecho y justicia',true),('Desarrollo social y comunitario',true),
('Economía y finanzas',true),('Contabilidad y auditoría',true),('Administración y emprendimiento',true),
('Mercadeo y comercio internacional',true),('Logística',true),('Lenguas y literatura',true),
('Matemática y estadística',true),('Física aplicada',true)
ON CONFLICT(nombre) DO NOTHING;
COMMIT;
