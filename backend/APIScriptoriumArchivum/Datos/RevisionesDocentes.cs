using APIScriptoriumArchivum.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Text.Json;
using System.Text.Json.Nodes;
using APIScriptoriumArchivum.Dtos;
using Npgsql;

namespace APIScriptoriumArchivum.Datos
{
    public class RevisionesDocentes
    {
        private IDbContextFactory<AppDbContext> baseDeDatos;
        private ArchivoPdf archivos;
        private Avisos avisos;
        public RevisionesDocentes(IDbContextFactory<AppDbContext> baseDeDatos, ArchivoPdf archivos, Avisos avisos)
        {
            this.baseDeDatos = baseDeDatos;
            this.archivos = archivos;
            this.avisos = avisos;
        }

        // Keep existing installed clients working while the repository panel uses the new routes.
        public async Task<object> Panel(int docente, bool super, CancellationToken ct)
        {
            var filas = await Listar(docente, null, "todas", null, ct, super);
            var trabajos = filas.Select(f =>
            {
                var t = JsonNode.Parse(f.GetRawText())!.AsObject();
                t["ejemplar_id"] = int.TryParse(t["id_entrega"]?.GetValue<string>(), out int id) ? JsonValue.Create(id) : null;
                t["resultado"] = t["revision_docente"]?["resultado"]?.DeepClone();
                t["pdf"] = t["pdf_disponible"]?.GetValue<bool>() == true ? JsonValue.Create("PDF actual") : null;
                return t;
            }).ToList();
            await using var c = await baseDeDatos.CreateDbContextAsync(ct);
            var alumnos = await Comandos.FilasJson(c, """
                SELECT jsonb_build_object('id',u.id_usuario,'nombre',coalesce(u.nombre_mostrar,concat_ws(' ',p.nombres,p.apellidos)),
                  'carrera',ca.nombre,'plan',pl.nombre)::text
                FROM public.usuarios u JOIN public.personas p USING(id_persona) JOIN public.roles r USING(id_rol)
                LEFT JOIN public.carreras ca USING(id_carrera) LEFT JOIN public.planes_estudio pl USING(id_plan)
                WHERE u.estado='Habilitada' AND r.activo AND r.nombre='Estudiante' AND (@super OR u.id_docente=@docente)
                ORDER BY p.apellidos,p.nombres
                """, ct, ("docente", docente), ("super", super));
            return new
            {
                alumnos,
                trabajos
            };
        }

        public async Task<JsonElement> Resumen(int docente, CancellationToken ct, bool super = false)
        {
            await using AppDbContext conexion = await baseDeDatos.CreateDbContextAsync(ct);
            List<JsonElement> filas = await Comandos.FilasJson(conexion, """
                WITH alumnos AS (
                    SELECT u.id_usuario, u.id_persona
                    FROM public.usuarios u
                    JOIN public.roles r USING (id_rol)
                    WHERE (@super OR u.id_docente = @docente) AND u.estado = 'Habilitada'
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
                """, ct, ("docente", docente), ("super", super));
            return filas[0];
        }

        public async Task<List<JsonElement>> Listar(int docente, string? busqueda, string? estado, int? estudiante, CancellationToken ct, bool super = false)
        {
            await using AppDbContext conexion = await baseDeDatos.CreateDbContextAsync(ct);
            List<JsonElement> filas = await Comandos.FilasJson(conexion, """
                WITH alumnos AS (
                    SELECT u.id_usuario, u.id_persona
                    FROM public.usuarios u
                    JOIN public.roles r USING (id_rol)
                    WHERE (@super OR u.id_docente = @docente) AND u.estado = 'Habilitada'
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
                        WHERE (@super OR u.id_docente = @docente) AND u.estado = 'Habilitada' AND r.nombre = 'Estudiante' AND r.activo
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
                    'autores', coalesce(to_jsonb(string_to_array(base.autores, '; ')), '[]'::jsonb),
                    'anio', base.anio,
                    'carrera', base.carrera,
                    'tipo', base.tipo,
                    'estado', replace(base.estado_publicacion, 'En revisión', 'En revision'),
                    'estudiante', base.estudiante,
                    'id_estudiante', base.id_estudiante::text,
                    'version', base.numero_version,
                    'id_entrega', base.id_entrega::text,
                    'enlace_proyecto', (SELECT ubicacion FROM public.recursos_digitales WHERE id_entrega=base.id_entrega AND clase='ENLACE' ORDER BY id_recurso LIMIT 1),
                    'pdf_disponible', EXISTS(SELECT 1 FROM public.recursos_digitales WHERE id_entrega=base.id_entrega AND clase='PDF' AND estado_carga='Disponible'),
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
                  AND (@estudiante::int IS NULL OR base.id_estudiante = @estudiante::int)
                ORDER BY base.fecha_registro DESC
                """, ct, ("docente", docente), ("super", super), ("q", busqueda ?? ""), ("estado", estado ?? "todas"), ("estudiante", estudiante));
            return filas;
        }

        public async Task<JsonElement> Detalle(int docente, int trabajo, CancellationToken ct, bool super = false)
        {
            await using AppDbContext conexion = await baseDeDatos.CreateDbContextAsync(ct);
            if (!await Asignado(conexion, null, docente, trabajo, ct, super))
            {
                throw new ErrorApi(404, "El trabajo no está asignado a tu cuenta.");
            }

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
                        WHERE (@super OR u.id_docente = @docente) AND u.estado = 'Habilitada' AND r.nombre = 'Estudiante' AND r.activo
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
                    'autores', coalesce(to_jsonb(string_to_array(base.autores, '; ')), '[]'::jsonb),
                    'anio', base.anio,
                    'carrera', base.carrera,
                    'tipo', base.tipo,
                    'estado', replace(base.estado_publicacion, 'En revisión', 'En revision'),
                    'estudiante', base.estudiante,
                    'id_estudiante', base.id_estudiante::text,
                    'version', base.numero_version,
                    'id_entrega', base.id_entrega::text,
                    'enlace_proyecto', (SELECT ubicacion FROM public.recursos_digitales WHERE id_entrega=base.id_entrega AND clase='ENLACE' ORDER BY id_recurso LIMIT 1),
                    'pdf_disponible', EXISTS(SELECT 1 FROM public.recursos_digitales WHERE id_entrega=base.id_entrega AND clase='PDF' AND estado_carga='Disponible'),
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
                """, ct, ("docente", docente), ("super", super), ("id", trabajo));
            return filas[0];
        }

        public async Task<JsonElement> Registrar(int docente, int trabajo, RevisionDocenteDatos datos, CancellationToken ct, bool super = false)
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

            await using AppDbContext conexion = await baseDeDatos.CreateDbContextAsync(ct);
            await using IDbContextTransaction tr = await conexion.Database.BeginTransactionAsync(ct);
            await Fichas.Bloquear(conexion, tr, trabajo, ct);
            string? estado = await Comandos.Valor<string>(conexion, tr, "SELECT estado_publicacion FROM public.trabajos WHERE id_trabajo=@id", ct, ("id", trabajo));
            if (estado is "Publicado" or "Retirado")
            {
                throw new ErrorApi(409, "Este trabajo solo permite consultar su historial.");
            }

            await Comandos.Ejecutar(conexion, tr, """
                SELECT u.id_usuario FROM public.usuarios u WHERE (@super OR u.id_docente=@docente)
                AND (u.id_usuario=(SELECT id_usuario_envio FROM public.trabajos WHERE id_trabajo=@id)
                  OR u.id_persona IN (SELECT id_persona FROM public.participaciones WHERE id_trabajo=@id AND calidad_participacion='Autor')) FOR SHARE
                """, ct, ("super", super), ("docente", docente), ("id", trabajo));
            if (!await Asignado(conexion, tr, docente, trabajo, ct, super))
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
            string? entregaRecibida = datos.EntregaId ?? datos.EjemplarId?.ToString();
            if (entregaRecibida is not null)
            {
                if (!int.TryParse(entregaRecibida, out int entregado) || entregado <= 0)
                {
                    throw new ErrorApi(400, "Indica un ejemplar válido.");
                }

                entrega = entregado;
            }

            if (entrega != actual)
            {
                await tr.RollbackAsync(CancellationToken.None);
                throw new ErrorApi(409, "La versión del trabajo cambió. Recarga la revisión.");
            }

            DateTime ahora = DateTime.UtcNow;
            var nueva = new APIScriptoriumArchivum.Models.Revisiones
            {
                IdEntrega = entrega,
                IdUsuarioRevisor = docente,
                FechaInicio = ahora,
                FechaCierre = ahora,
                AmbitoRevision = "Revisión docente",
                Resultado = datos.Resultado,
                MotivoDecision = observaciones
            };
            conexion.Revisiones.Add(nueva);
            await conexion.SaveChangesAsync(ct);
            List<JsonElement> creadas = await Comandos.FilasJson(conexion, tr, """
                SELECT jsonb_build_object(
                    'id_revision', n.id_revision::text,
                    'id_entrega', n.id_entrega::text,
                    'resultado', n.resultado,
                    'observaciones', n.motivo_decision,
                    'fecha', n.fecha_cierre,
                    'docente', coalesce(ru.nombre_mostrar, concat_ws(' ', rp.nombres, rp.apellidos))
                )::text
                FROM public.revisiones n
                LEFT JOIN public.usuarios ru ON ru.id_usuario = n.id_usuario_revisor
                LEFT JOIN public.personas rp ON rp.id_persona = ru.id_persona
                WHERE n.id_revision=@revision
                """, ct, ("revision", nueva.IdRevision));
            JsonElement revision = creadas[0];
            await avisos.Trabajo(conexion, tr, trabajo, docente, "REVISION_DOCENTE", "Revisión del docente revisor", $"Resultado: {datos.Resultado}\nObservaciones: {observaciones}", true, ct);
            await tr.CommitAsync(CancellationToken.None);
            return revision;
        }

        public async Task<string> Pdf(int docente, int trabajo, CancellationToken ct, bool super = false, int? ejemplar = null)
        {
            await using AppDbContext conexion = await baseDeDatos.CreateDbContextAsync(ct);
            if (!await Asignado(conexion, null, docente, trabajo, ct, super))
            {
                throw new ErrorApi(404, "El trabajo no existe o no está asignado a tus estudiantes.");
            }

            string? clave = await Comandos.Valor<string>(conexion, null, """
                SELECT r.ubicacion FROM public.entregas e
                JOIN public.recursos_digitales r USING (id_entrega, id_trabajo)
                WHERE e.id_trabajo = @id
                  AND e.numero_version = (SELECT max(numero_version) FROM public.entregas WHERE id_trabajo = @id)
                  AND r.clase = 'PDF' AND r.estado_carga = 'Disponible'
                  AND (@ejemplar::int IS NULL OR e.id_entrega=@ejemplar::int)
                """, ct, ("id", trabajo), ("ejemplar", ejemplar));
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

        public async Task Autorizar(int trabajo, int docente, bool super, CancellationToken ct)
        {
            await using var c = await baseDeDatos.CreateDbContextAsync(ct);
            if (!await Asignado(c, null, docente, trabajo, ct, super))
            {
                throw new ErrorApi(404, "El trabajo no está asignado a tu cuenta.");
            }
        }

        public async Task<List<JsonElement>> Historial(int trabajo, CancellationToken ct)
        {
            await using var c = await baseDeDatos.CreateDbContextAsync(ct);
            return await Comandos.FilasJson(c, """
                SELECT jsonb_build_object('id',r.id_revision::text,'ejemplar_id',r.id_entrega,'version',e.numero_version,
                  'vigente',r.id_entrega=(SELECT id_entrega FROM public.entregas WHERE id_trabajo=@id ORDER BY numero_version DESC LIMIT 1),
                  'docente',coalesce(u.nombre_mostrar,concat_ws(' ',p.nombres,p.apellidos)),
                  'resultado',r.resultado,'observaciones',r.motivo_decision,'fecha',r.fecha_cierre)::text
                FROM public.revisiones r JOIN public.entregas e USING(id_entrega)
                JOIN public.usuarios u ON u.id_usuario=r.id_usuario_revisor JOIN public.personas p USING(id_persona)
                WHERE e.id_trabajo=@id AND r.ambito_revision='Revisión docente' AND r.fecha_cierre IS NOT NULL
                ORDER BY r.fecha_cierre DESC,r.id_revision DESC
                """, ct, ("id", trabajo));
        }

        public static async Task<bool> Asignado(AppDbContext conexion, IDbContextTransaction? transaccion, int docente, int trabajo, CancellationToken ct, bool super = false)
        {
            return await Comandos.Valor<bool>(conexion, transaccion, """
                SELECT EXISTS(
                    SELECT 1 FROM public.usuarios u
                    JOIN public.roles r ON r.id_rol = u.id_rol
                    WHERE (@super OR u.id_docente = @docente) AND u.estado = 'Habilitada'
                      AND r.nombre = 'Estudiante' AND r.activo
                      AND (
                        u.id_usuario = (SELECT id_usuario_envio FROM public.trabajos WHERE id_trabajo = @id)
                        OR u.id_persona IN (
                            SELECT ap.id_persona FROM public.participaciones ap
                            WHERE ap.id_trabajo = @id AND ap.calidad_participacion = 'Autor')
                      )
                )
                """, ct, ("docente", docente), ("super", super), ("id", trabajo));
        }
    }
}
