using System.Text.Json;
using APIScriptoriumArchivum.Dtos;
using Npgsql;

namespace APIScriptoriumArchivum.Datos
{
    public class RevisionesDocentes
    {
        private NpgsqlDataSource baseDeDatos;
        private ArchivoPdf archivos;
        private Avisos avisos;

        public RevisionesDocentes(NpgsqlDataSource baseDeDatos, ArchivoPdf archivos, Avisos avisos)
        {
            this.baseDeDatos = baseDeDatos;
            this.archivos = archivos;
            this.avisos = avisos;
        }

        public async Task<JsonElement> Resumen(int docente, CancellationToken ct)
        {
            await using NpgsqlConnection conexion = await baseDeDatos.OpenConnectionAsync(ct);
            List<JsonElement> filas = await Comandos.FilasJson(conexion, """
                WITH alumnos AS (
                    SELECT u.id_usuario, u.id_persona
                    FROM public.usuarios u
                    JOIN public.roles r USING (id_rol)
                    WHERE u.id_docente = @docente AND u.estado = 'Habilitada'
                      AND r.nombre = 'Estudiante' AND r.activo
                ),
                base AS (
                    SELECT t.id_trabajo, e.id_entrega, e.numero_version,
                           rd.id_revision, rd.resultado AS resultado_docente
                    FROM public.trabajos t
                    LEFT JOIN LATERAL (
                        SELECT x.* FROM public.entregas x
                        WHERE x.id_trabajo = t.id_trabajo
                        ORDER BY x.numero_version DESC
                        LIMIT 1
                    ) e ON true
                    LEFT JOIN LATERAL (
                        SELECT r.* FROM public.revisiones r
                        WHERE r.id_entrega = e.id_entrega
                          AND r.ambito_revision = 'Revisión docente' AND r.fecha_cierre IS NOT NULL
                        ORDER BY r.fecha_cierre DESC, r.id_revision DESC
                        LIMIT 1
                    ) rd ON true
                    WHERE t.id_usuario_envio IN (SELECT id_usuario FROM alumnos)
                       OR EXISTS (
                            SELECT 1 FROM public.participaciones ap
                            WHERE ap.id_trabajo = t.id_trabajo AND ap.calidad_participacion = 'Autor'
                              AND ap.id_persona IN (SELECT id_persona FROM alumnos))
                )
                SELECT jsonb_build_object(
                    'docentes', (SELECT count(*) FROM alumnos),
                    'pendientes', (SELECT count(*) FROM base WHERE base.id_revision IS NULL),
                    'conformes', (SELECT count(*) FROM base WHERE base.id_revision IS NOT NULL AND base.resultado_docente = 'Conforme'),
                    'cambios', (SELECT count(*) FROM base WHERE base.id_revision IS NOT NULL AND base.resultado_docente = 'Requiere cambios')
                )::text AS datos
                FROM base
                LIMIT 1
                """, ct, ("docente", docente));
            return filas[0];
        }

        public async Task<List<JsonElement>> Listar(int docente, string? busqueda, string? estado, int? estudiante, CancellationToken ct)
        {
            await using NpgsqlConnection conexion = await baseDeDatos.OpenConnectionAsync(ct);
            List<JsonElement> filas = await Comandos.FilasJson(conexion, """
                WITH alumnos AS (
                    SELECT u.id_usuario, u.id_persona
                    FROM public.usuarios u
                    JOIN public.roles r USING (id_rol)
                    WHERE u.id_docente = @docente AND u.estado = 'Habilitada'
                      AND r.nombre = 'Estudiante' AND r.activo
                ),
                base AS (
                    SELECT t.id_trabajo, t.titulo, t.anio, t.estado_publicacion, t.fecha_registro,
                           c.nombre AS carrera, tt.nombre AS tipo,
                           e.id_entrega, e.numero_version,
                           est.id_usuario AS id_estudiante, est.nombre AS estudiante,
                           (SELECT array_to_string(array_agg(concat_ws(' ', pe.nombres, pe.apellidos) ORDER BY ap.orden_autoria NULLS LAST, ap.id_participacion), '; ')
                               FROM public.participaciones ap
                               JOIN public.personas pe USING (id_persona)
                               WHERE ap.id_trabajo = t.id_trabajo AND ap.calidad_participacion = 'Autor') AS autores,
                           rd.id_revision, rd.resultado AS resultado_docente, rd.motivo_decision AS observaciones,
                           rd.fecha_cierre AS fecha_revision, rd.nombre_docente
                    FROM public.trabajos t
                    JOIN public.carreras c USING (id_carrera)
                    JOIN public.tipos_trabajo tt USING (id_tipo_trabajo)
                    LEFT JOIN LATERAL (
                        SELECT x.* FROM public.entregas x
                        WHERE x.id_trabajo = t.id_trabajo
                        ORDER BY x.numero_version DESC
                        LIMIT 1
                    ) e ON true
                    LEFT JOIN LATERAL (
                        SELECT u.id_usuario, coalesce(u.nombre_mostrar, concat_ws(' ', p.nombres, p.apellidos)) AS nombre
                        FROM public.usuarios u
                        JOIN public.personas p ON p.id_persona = u.id_persona
                        JOIN public.roles r ON r.id_rol = u.id_rol
                        WHERE u.id_docente = @docente AND u.estado = 'Habilitada' AND r.nombre = 'Estudiante' AND r.activo
                          AND (u.id_usuario = t.id_usuario_envio OR u.id_persona IN (
                                SELECT ap.id_persona FROM public.participaciones ap
                                WHERE ap.id_trabajo = t.id_trabajo AND ap.calidad_participacion = 'Autor'))
                        ORDER BY (u.id_usuario = t.id_usuario_envio) DESC
                        LIMIT 1
                    ) est ON true
                    LEFT JOIN LATERAL (
                        SELECT r.*, coalesce(ru.nombre_mostrar, concat_ws(' ', rp.nombres, rp.apellidos)) AS nombre_docente
                        FROM public.revisiones r
                        LEFT JOIN public.usuarios ru ON ru.id_usuario = r.id_usuario_revisor
                        LEFT JOIN public.personas rp ON rp.id_persona = ru.id_persona
                        WHERE r.id_entrega = e.id_entrega
                          AND r.ambito_revision = 'Revisión docente' AND r.fecha_cierre IS NOT NULL
                        ORDER BY r.fecha_cierre DESC, r.id_revision DESC
                        LIMIT 1
                    ) rd ON true
                    WHERE t.id_usuario_envio IN (SELECT id_usuario FROM alumnos)
                       OR EXISTS (
                            SELECT 1 FROM public.participaciones ap
                            WHERE ap.id_trabajo = t.id_trabajo AND ap.calidad_participacion = 'Autor'
                              AND ap.id_persona IN (SELECT id_persona FROM alumnos))
                )
                SELECT jsonb_build_object(
                    'id', base.id_trabajo::text,
                    'titulo', base.titulo,
                    'autores', base.autores,
                    'anio', base.anio,
                    'carrera', base.carrera,
                    'tipo', base.tipo,
                    'estado', replace(base.estado_publicacion, 'En revisión', 'En revision'),
                    'estudiante', base.estudiante,
                    'id_estudiante', base.id_estudiante::text,
                    'version', base.numero_version,
                    'id_entrega', base.id_entrega::text,
                    'estado_revision', CASE WHEN base.id_revision IS NULL THEN 'pendiente' WHEN base.resultado_docente = 'Conforme' THEN 'conforme' ELSE 'cambios' END,
                    'revision_docente', CASE WHEN base.id_revision IS NULL THEN null ELSE jsonb_build_object(
                        'resultado', base.resultado_docente,
                        'observaciones', base.observaciones,
                        'fecha', base.fecha_revision,
                        'docente', base.nombre_docente,
                        'version', base.numero_version) END
                )::text AS datos
                FROM base
                WHERE (@q = '' OR strpos(lower(base.titulo), lower(@q)) > 0 OR strpos(lower(base.autores), lower(@q)) > 0 OR strpos(lower(base.estudiante), lower(@q)) > 0)
                  AND (@estado = 'todas' OR (@estado = 'pendiente' AND base.id_revision IS NULL) OR (@estado = 'conforme' AND base.resultado_docente = 'Conforme') OR (@estado = 'cambios' AND base.resultado_docente = 'Requiere cambios'))
                  AND (@estudiante IS NULL OR base.id_estudiante = @estudiante)
                ORDER BY base.fecha_registro DESC
                """, ct, ("docente", docente), ("q", busqueda ?? ""), ("estado", estado ?? "todas"), ("estudiante", estudiante));
            return filas;
        }

        public async Task<JsonElement> Detalle(int docente, int trabajo, CancellationToken ct)
        {
            await using NpgsqlConnection conexion = await baseDeDatos.OpenConnectionAsync(ct);
            List<JsonElement> filas = await Comandos.FilasJson(conexion, """
                WITH base AS (
                    SELECT t.id_trabajo, t.titulo, t.anio, t.estado_publicacion, t.fecha_registro,
                           c.nombre AS carrera, tt.nombre AS tipo,
                           e.id_entrega, e.numero_version,
                           est.id_usuario AS id_estudiante, est.nombre AS estudiante,
                           (SELECT array_to_string(array_agg(concat_ws(' ', pe.nombres, pe.apellidos) ORDER BY ap.orden_autoria NULLS LAST, ap.id_participacion), '; ')
                               FROM public.participaciones ap
                               JOIN public.personas pe USING (id_persona)
                               WHERE ap.id_trabajo = t.id_trabajo AND ap.calidad_participacion = 'Autor') AS autores,
                           rd.id_revision, rd.resultado AS resultado_docente, rd.motivo_decision AS observaciones,
                           rd.fecha_cierre AS fecha_revision, rd.nombre_docente
                    FROM public.trabajos t
                    JOIN public.carreras c USING (id_carrera)
                    JOIN public.tipos_trabajo tt USING (id_tipo_trabajo)
                    LEFT JOIN LATERAL (
                        SELECT x.* FROM public.entregas x
                        WHERE x.id_trabajo = t.id_trabajo
                        ORDER BY x.numero_version DESC
                        LIMIT 1
                    ) e ON true
                    LEFT JOIN LATERAL (
                        SELECT u.id_usuario, coalesce(u.nombre_mostrar, concat_ws(' ', p.nombres, p.apellidos)) AS nombre
                        FROM public.usuarios u
                        JOIN public.personas p ON p.id_persona = u.id_persona
                        JOIN public.roles r ON r.id_rol = u.id_rol
                        WHERE u.id_docente = @docente AND u.estado = 'Habilitada' AND r.nombre = 'Estudiante' AND r.activo
                          AND (u.id_usuario = t.id_usuario_envio OR u.id_persona IN (
                                SELECT ap.id_persona FROM public.participaciones ap
                                WHERE ap.id_trabajo = t.id_trabajo AND ap.calidad_participacion = 'Autor'))
                        ORDER BY (u.id_usuario = t.id_usuario_envio) DESC
                        LIMIT 1
                    ) est ON true
                    LEFT JOIN LATERAL (
                        SELECT r.*, coalesce(ru.nombre_mostrar, concat_ws(' ', rp.nombres, rp.apellidos)) AS nombre_docente
                        FROM public.revisiones r
                        LEFT JOIN public.usuarios ru ON ru.id_usuario = r.id_usuario_revisor
                        LEFT JOIN public.personas rp ON rp.id_persona = ru.id_persona
                        WHERE r.id_entrega = e.id_entrega
                          AND r.ambito_revision = 'Revisión docente' AND r.fecha_cierre IS NOT NULL
                        ORDER BY r.fecha_cierre DESC, r.id_revision DESC
                        LIMIT 1
                    ) rd ON true
                    WHERE t.id_trabajo = @id
                )
                SELECT jsonb_build_object(
                    'id', base.id_trabajo::text,
                    'titulo', base.titulo,
                    'autores', base.autores,
                    'anio', base.anio,
                    'carrera', base.carrera,
                    'tipo', base.tipo,
                    'estado', replace(base.estado_publicacion, 'En revisión', 'En revision'),
                    'estudiante', base.estudiante,
                    'id_estudiante', base.id_estudiante::text,
                    'version', base.numero_version,
                    'id_entrega', base.id_entrega::text,
                    'estado_revision', CASE WHEN base.id_revision IS NULL THEN 'pendiente' WHEN base.resultado_docente = 'Conforme' THEN 'conforme' ELSE 'cambios' END,
                    'revision_docente', CASE WHEN base.id_revision IS NULL THEN null ELSE jsonb_build_object(
                        'resultado', base.resultado_docente,
                        'observaciones', base.observaciones,
                        'fecha', base.fecha_revision,
                        'docente', base.nombre_docente,
                        'version', base.numero_version) END,
                    'historial', (SELECT COALESCE(jsonb_agg(jsonb_build_object(
                            'resultado', r.resultado,
                            'observaciones', r.motivo_decision,
                            'fecha', r.fecha_cierre,
                            'docente', coalesce(ru.nombre_mostrar, concat_ws(' ', rp.nombres, rp.apellidos)),
                            'version', er.numero_version) ORDER BY r.fecha_cierre, r.id_revision), '[]'::jsonb)
                        FROM public.revisiones r
                        JOIN public.entregas er ON er.id_entrega = r.id_entrega
                        LEFT JOIN public.usuarios ru ON ru.id_usuario = r.id_usuario_revisor
                        LEFT JOIN public.personas rp ON rp.id_persona = ru.id_persona
                        WHERE r.id_entrega IN (SELECT id_entrega FROM public.entregas WHERE id_trabajo = @id)
                          AND r.ambito_revision = 'Revisión docente' AND r.fecha_cierre IS NOT NULL)
                )::text AS datos
                FROM base
                """, ct, ("docente", docente), ("id", trabajo));
            return filas[0];
        }

        public async Task<JsonElement> Registrar(int docente, int trabajo, RevisionDocenteDatos datos, CancellationToken ct)
        {
            if (datos.Resultado is not ("Conforme" or "Requiere cambios"))
            {
                throw new ErrorApi(400, "La revisión debe registrar Conforme o Requiere cambios.");
            }
            string observaciones = (datos.Observaciones ?? string.Empty).Trim();
            if (observaciones.Length is < 10 or > 5000)
            {
                throw new ErrorApi(400, "Las observaciones deben tener entre 10 y 5000 caracteres.");
            }
            await using NpgsqlConnection conexion = await baseDeDatos.OpenConnectionAsync(ct);
            await using NpgsqlTransaction tr = await conexion.BeginTransactionAsync(ct);
            if (!await Asignado(conexion, tr, docente, trabajo, ct))
            {
                await tr.RollbackAsync(CancellationToken.None);
                throw new ErrorApi(404, "El trabajo no existe o no está asignado a tus estudiantes.");
            }
            int? actual = await Comandos.Valor<int>(conexion, tr, """
                SELECT e.id_entrega FROM public.entregas e
                WHERE e.id_trabajo = @id
                ORDER BY e.numero_version DESC
                LIMIT 1
                """, ct, ("id", trabajo));
            if (actual is null || actual == 0)
            {
                await tr.RollbackAsync(CancellationToken.None);
                throw new ErrorApi(409, "El trabajo no tiene una entrega para revisar.");
            }
            int entrega = actual ?? 0;
            if (datos.EntregaId is not null && int.TryParse(datos.EntregaId.Trim(), out int entregado) && entregado > 0)
            {
                entrega = entregado;
            }
            if (entrega != actual)
            {
                await tr.RollbackAsync(CancellationToken.None);
                throw new ErrorApi(409, "La versión del trabajo cambió. Recargá la revisión.");
            }

            List<JsonElement> creadas = await Comandos.FilasJson(conexion, tr, """
                WITH nueva AS (
                    INSERT INTO public.revisiones(id_entrega, id_usuario_revisor, fecha_inicio, fecha_cierre, ambito_revision, resultado, motivo_decision)
                    VALUES (@entrega, @actor, clock_timestamp(), clock_timestamp(), 'Revisión docente', @resultado, @observaciones)
                    RETURNING id_revision, id_entrega
                )
                SELECT jsonb_build_object(
                    'id_revision', n.id_revision::text,
                    'id_entrega', n.id_entrega::text,
                    'resultado', n.resultado,
                    'observaciones', n.motivo_decision,
                    'fecha', n.fecha_cierre,
                    'docente', coalesce(ru.nombre_mostrar, concat_ws(' ', rp.nombres, rp.apellidos))
                )::text AS datos
                FROM nueva n
                LEFT JOIN public.usuarios ru ON ru.id_usuario = n.id_usuario_revisor
                LEFT JOIN public.personas rp ON rp.id_persona = ru.id_persona
                """, ct, ("entrega", entrega), ("actor", docente), ("resultado", datos.Resultado), ("observaciones", observaciones));
            JsonElement revision = creadas[0];

            await avisos.Trabajo(conexion, tr, trabajo, docente, "REVISION_DOCENTE", "Revisión del docente revisor",
                $"Resultado: {datos.Resultado}\nObservaciones: {observaciones}", true, ct);
            await tr.CommitAsync(CancellationToken.None);
            return revision;
        }

        public async Task<string> Pdf(int docente, int trabajo, CancellationToken ct)
        {
            await using NpgsqlConnection conexion = await baseDeDatos.OpenConnectionAsync(ct);
            if (!await Asignado(conexion, null, docente, trabajo, ct))
            {
                throw new ErrorApi(404, "El trabajo no existe o no está asignado a tus estudiantes.");
            }
            string? clave = await Comandos.Valor<string>(conexion, null, """
                SELECT r.ubicacion FROM public.entregas e
                JOIN public.recursos_digitales r USING (id_entrega, id_trabajo)
                WHERE e.id_trabajo = @id
                  AND e.numero_version = (SELECT max(numero_version) FROM public.entregas WHERE id_trabajo = @id)
                  AND r.clase = 'PDF' AND r.estado_carga = 'Disponible'
                """, ct, ("id", trabajo));
            if (clave is null)
            {
                throw new ErrorApi(404, "El ejemplar no tiene un PDF disponible.");
            }
            string? ruta = archivos.Ubicar(clave);
            if (ruta is null)
            {
                throw new ErrorApi(404, "El ejemplar no tiene un PDF disponible.");
            }
            return ruta;
        }

        public static async Task<bool> Asignado(NpgsqlConnection conexion, NpgsqlTransaction? transaccion, int docente, int trabajo, CancellationToken ct)
        {
            await using NpgsqlCommand comando = Comandos.Crear(conexion, transaccion, """
                SELECT EXISTS(
                    SELECT 1 FROM public.usuarios u
                    JOIN public.roles r ON r.id_rol = u.id_rol
                    WHERE u.id_docente = @docente AND u.estado = 'Habilitada'
                      AND r.nombre = 'Estudiante' AND r.activo
                      AND (
                        u.id_usuario = (SELECT id_usuario_envio FROM public.trabajos WHERE id_trabajo = @id)
                        OR u.id_persona IN (
                            SELECT ap.id_persona FROM public.participaciones ap
                            WHERE ap.id_trabajo = @id AND ap.calidad_participacion = 'Autor')
                      )
                )
                """, ("docente", docente), ("id", trabajo));
            await using NpgsqlDataReader lector = await comando.ExecuteReaderAsync(ct);
            return await lector.ReadAsync(ct) && lector.GetFieldValue<bool>(0);
        }
    }
}
