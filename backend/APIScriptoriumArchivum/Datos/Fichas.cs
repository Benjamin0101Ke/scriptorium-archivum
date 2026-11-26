using APIScriptoriumArchivum.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Globalization;
using System.Text;
using System.Text.Json;
using APIScriptoriumArchivum.Dtos;
using Npgsql;

namespace APIScriptoriumArchivum.Datos
{
    public class Fichas
    {
        public const string DatosBasicos = """
            'id', t.id_trabajo::text, 'titulo', t.titulo, 'anio', t.anio, 'resumen', t.resumen,
            'carrera', c.nombre, 'tipo', tt.nombre, 'area', array_to_string(a.nombres, '; '),
            'areas', a.nombres, 'area_ids', a.ids, 'id_carrera', t.id_carrera,
            'id_tipo_trabajo', t.id_tipo_trabajo, 'autores', p.nombres, 'autor_ids', p.ids
            """;
        public const string RelacionesBasicas = """
            FROM public.trabajos t
            JOIN public.carreras c USING (id_carrera)
            JOIN public.tipos_trabajo tt USING (id_tipo_trabajo)
            CROSS JOIN LATERAL (
                SELECT array(SELECT at.nombre FROM public.trabajos_areas ta
                        JOIN public.areas_tematicas at USING(id_area) WHERE ta.id_trabajo=t.id_trabajo ORDER BY at.nombre) nombres,
                       array(SELECT ta.id_area FROM public.trabajos_areas ta WHERE ta.id_trabajo=t.id_trabajo ORDER BY ta.id_area) ids
            ) a
            CROSS JOIN LATERAL (
                SELECT array(SELECT concat_ws(' ', pe.nombres, pe.apellidos) FROM public.participaciones ap
                        JOIN public.personas pe USING(id_persona) WHERE ap.id_trabajo=t.id_trabajo AND ap.calidad_participacion='Autor'
                        ORDER BY ap.orden_autoria NULLS LAST, ap.id_participacion) nombres,
                       array(SELECT ap.id_persona FROM public.participaciones ap WHERE ap.id_trabajo=t.id_trabajo
                        AND ap.calidad_participacion='Autor' ORDER BY ap.orden_autoria NULLS LAST, ap.id_participacion) ids
            ) p
            """;
        private IDbContextFactory<AppDbContext> baseDeDatos;
        private Avisos avisos;
        public Fichas(IDbContextFactory<AppDbContext> baseDeDatos, Avisos avisos)
        {
            this.baseDeDatos = baseDeDatos;
            this.avisos = avisos;
        }

        public async Task<List<JsonElement>> Listar(CancellationToken ct)
        {
            await using AppDbContext conexion = await baseDeDatos.CreateDbContextAsync(ct);
            string sql = "SELECT jsonb_build_object(" + DatosBasicos + ", 'estado', replace(t.estado_publicacion, 'En revisión', 'En revision'), 'revision_docente', CASE WHEN rd.id_revision IS NULL THEN null ELSE jsonb_build_object('resultado',rd.resultado,'observaciones',rd.motivo_decision,'fecha',rd.fecha_cierre,'docente',coalesce(ru.nombre_mostrar,concat_ws(' ',rp.nombres,rp.apellidos)),'version',rd.numero_version) END)::text " + RelacionesBasicas + " LEFT JOIN LATERAL (SELECT r.*, e.numero_version FROM public.entregas e JOIN public.revisiones r ON r.id_entrega=e.id_entrega WHERE e.id_trabajo=t.id_trabajo AND r.ambito_revision='Revisión docente' AND r.fecha_cierre IS NOT NULL ORDER BY e.numero_version DESC, r.fecha_cierre DESC, r.id_revision DESC LIMIT 1) rd ON true LEFT JOIN public.usuarios ru ON ru.id_usuario=rd.id_usuario_revisor LEFT JOIN public.personas rp ON rp.id_persona=ru.id_persona ORDER BY t.id_trabajo DESC";
            return await Comandos.FilasJson(conexion, sql, ct);
        }

        public async Task<List<JsonElement>> Personas(string? busqueda, CancellationToken ct)
        {
            await using AppDbContext conexion = await baseDeDatos.CreateDbContextAsync(ct);
            const string sql = """
                SELECT jsonb_build_object('id', id_persona, 'nombre', concat_ws(' ', nombres, apellidos),
                                          'nombres', nombres, 'apellidos', apellidos)::text
                FROM public.personas WHERE @q = '' OR strpos(lower(concat_ws(' ', nombres, apellidos)), lower(@q)) > 0
                ORDER BY nombres, apellidos, id_persona LIMIT 100
                """;
            return await Comandos.FilasJson(conexion, sql, ct, ("q", busqueda?.Trim() ?? ""));
        }

        public async Task<int> CrearPersona(PersonaDatos datos, CancellationToken ct)
        {
            string nombres = datos.Nombres?.Trim() ?? "";
            string apellidos = datos.Apellidos?.Trim() ?? "";
            string? carnet = TextoOpcional(datos.IdentificadorInstitucional);
            if (nombres.Length is 0 or > 100 || apellidos.Length is 0 or > 100 || carnet?.Length > 30)
            {
                throw new ErrorApi(400, "Revisa los nombres, apellidos y el identificador institucional.");
            }

            await using AppDbContext conexion = await baseDeDatos.CreateDbContextAsync(ct);
            try
            {
                var persona = new APIScriptoriumArchivum.Models.Personas
                {
                    Nombres = nombres,
                    Apellidos = apellidos,
                    IdentificadorInstitucional = carnet
                };
                conexion.Personas.Add(persona);
                await conexion.SaveChangesAsync(ct);
                return persona.IdPersona;
            }
            catch (DbUpdateException e)when (e.InnerException is PostgresException postgres && postgres.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                throw new ErrorApi(409, "Ya existe una persona con ese identificador.");
            }
        }

        public async Task<int> Guardar(int? id, MetadatosFicha datos, CancellationToken ct, int? actor = null)
        {
            await using var conexion = await baseDeDatos.CreateDbContextAsync(ct);
            await using var tr = await conexion.Database.BeginTransactionAsync(ct);
            int ficha = await Guardar(conexion, tr, id, datos, ct);
            if (id.HasValue && actor.HasValue)
            {
                await avisos.Trabajo(conexion, tr, ficha, actor.Value, "METADATOS_ACTUALIZADOS", "Datos del trabajo actualizados", "Biblioteca guardó una corrección de los datos de la ficha. Consulta los datos actualizados en el repositorio.", false, ct);
            }

            await tr.CommitAsync(ct);
            return ficha;
        }

        internal async Task<int> Guardar(AppDbContext conexion, IDbContextTransaction tr, int? id, MetadatosFicha datos, CancellationToken ct, int? actorEnvio = null)
        {
            string titulo = datos.Titulo?.Trim() ?? "";
            string resumen = datos.Resumen?.Trim() ?? "";
            List<string> invalidos = new List<string>();
            if (titulo.Length is 0 or > 250)
            {
                invalidos.Add("titulo");
            }

            if (datos.Anio is < 1000 or > 9999)
            {
                invalidos.Add("anio");
            }

            if (resumen.Length == 0)
            {
                invalidos.Add("resumen");
            }

            if (id.HasValue)
            {
                await Bloquear(conexion, tr, id.Value, ct);
            }

            int carrera = await Comandos.Valor<int>(conexion, tr, """
                SELECT id_carrera FROM public.carreras WHERE activa
                AND ((@id::int IS NOT NULL AND id_carrera=@id::int) OR (@id::int IS NULL AND lower(nombre)=lower(@nombre)))
                """, ct, ("id", datos.IdCarrera), ("nombre", datos.Carrera?.Trim() ?? ""));
            int tipo = await Comandos.Valor<int>(conexion, tr, """
                SELECT id_tipo_trabajo FROM public.tipos_trabajo
                WHERE (activa OR id_tipo_trabajo=(SELECT id_tipo_trabajo FROM public.trabajos WHERE id_trabajo=@ficha::int))
                AND ((@id::int IS NOT NULL AND id_tipo_trabajo=@id::int) OR (@id::int IS NULL AND lower(nombre)=lower(@nombre)))
                """, ct, ("id", datos.IdTipoTrabajo), ("nombre", datos.Tipo?.Trim() ?? ""), ("ficha", id));
            int[] areas = datos.AreaIds?.Distinct().ToArray() ?? await AreasPorNombre(conexion, tr, datos.Area, ct);
            int[] autores = await ResolverPersonas(conexion, tr, datos.AutorIds, datos.Autores, ct);
            if (carrera == 0)
            {
                invalidos.Add("carrera");
            }

            if (tipo == 0)
            {
                invalidos.Add("tipo");
            }

            if (areas.Length != await Comandos.Valor<long>(conexion, tr, "SELECT count(*) FROM public.areas_tematicas WHERE activa AND id_area=ANY(@ids)", ct, ("ids", areas)))
            {
                invalidos.Add("area");
            }

            if (autores.Length == 0)
            {
                invalidos.Add("autores");
            }

            if (invalidos.Count > 0)
            {
                throw new ErrorApi(400, "Los metadatos de la ficha no están completos.", new { error = "Revisa los campos y usa personas y catálogos ya registrados.", fields = invalidos });
            }

            if (!id.HasValue && !datos.ConfirmarDuplicado)
            {
                const string posibles = """
                    SELECT t.titulo FROM public.trabajos t WHERE t.anio=@anio AND
                    array(SELECT p.id_persona FROM public.participaciones p
                          WHERE p.id_trabajo=t.id_trabajo AND calidad_participacion='Autor' ORDER BY p.id_persona)=@autores
                    """;
                List<string> titulos = await Comandos.Filas<string>(conexion, posibles, ct, ("anio", datos.Anio), ("autores", autores.Order().ToArray()));
                foreach (string existente in titulos)
                {
                    if (Normalizar(existente) == Normalizar(titulo))
                    {
                        throw new ErrorApi(409, "Posible ficha duplicada.", new { error = "Posible ficha duplicada.", duplicate = true });
                    }
                }
            }

            APIScriptoriumArchivum.Models.Trabajos trabajo;
            if (id.HasValue)
            {
                trabajo = await conexion.Trabajos.SingleAsync(t => t.IdTrabajo == id.Value, ct);
                await conexion.Participaciones.Where(p => p.IdTrabajo == id.Value && p.CalidadParticipacion == "Autor").ExecuteDeleteAsync(ct);
                await conexion.TrabajosAreas.Where(a => a.IdTrabajo == id.Value).ExecuteDeleteAsync(ct);
            }
            else
            {
                trabajo = new APIScriptoriumArchivum.Models.Trabajos
                {
                    Codigo = "TR-" + Guid.NewGuid().ToString()[..23],
                    IdUsuarioEnvio = actorEnvio,
                    EstadoPublicacion = actorEnvio.HasValue ? "En revisión" : "Borrador"
                };
                conexion.Trabajos.Add(trabajo);
            }

            trabajo.Titulo = titulo;
            trabajo.Resumen = resumen;
            trabajo.Anio = (short)datos.Anio;
            trabajo.IdCarrera = carrera;
            trabajo.IdTipoTrabajo = tipo;
            await conexion.SaveChangesAsync(ct);
            int ficha = trabajo.IdTrabajo;
            for (int posicion = 0; posicion < autores.Length; posicion++)
            {
                conexion.Participaciones.Add(new APIScriptoriumArchivum.Models.Participaciones { IdTrabajo = ficha, IdPersona = autores[posicion], CalidadParticipacion = "Autor", OrdenAutoria = checked((short)(posicion + 1)) });
            }

            foreach (int area in areas)
            {
                conexion.TrabajosAreas.Add(new APIScriptoriumArchivum.Models.TrabajosAreas { IdTrabajo = ficha, IdArea = area });
            }

            await conexion.SaveChangesAsync(ct);
            return ficha;
        }

        internal static async Task<string> Bloquear(AppDbContext conexion, IDbContextTransaction tr, int id, CancellationToken ct)
        {
            string? estado = await Comandos.Valor<string>(conexion, tr, "SELECT estado_publicacion FROM public.trabajos WHERE id_trabajo=@id FOR UPDATE", ct, ("id", id));
            if (estado is null)
            {
                throw new ErrorApi(404, "La ficha no existe.");
            }

            if (estado == "Retirado")
            {
                throw new ErrorApi(409, "La ficha está retirada.");
            }

            return estado;
        }

        internal static async Task<int[]> ResolverPersonas(AppDbContext conexion, IDbContextTransaction tr, int[]? ids, string[]? nombres, CancellationToken ct)
        {
            if (ids is not null)
            {
                int[] personas = ids.Distinct().ToArray();
                long encontradas = await Comandos.Valor<long>(conexion, tr, "SELECT count(*) FROM public.personas WHERE id_persona=ANY(@ids)", ct, ("ids", personas));
                return encontradas == personas.Length ? personas : Array.Empty<int>();
            }

            List<int> resultado = new List<int>();
            foreach (string nombre in nombres ?? Array.Empty<string>())
            {
                int[] encontrados = await Comandos.Valor<int[]>(conexion, tr, """
                    SELECT array(SELECT id_persona FROM public.personas
                        WHERE lower(concat_ws(' ',nombres,apellidos))=lower(@nombre))
                    """, ct, ("nombre", nombre?.Trim() ?? "")) ?? Array.Empty<int>();
                if (encontrados.Length != 1)
                {
                    return Array.Empty<int>();
                }

                resultado.Add(encontrados[0]);
            }

            return resultado.Distinct().ToArray();
        }

        private static async Task<int[]> AreasPorNombre(AppDbContext conexion, IDbContextTransaction tr, string? dato, CancellationToken ct)
        {
            string[] nombres = (dato ?? "").Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Select(n => n.ToLowerInvariant()).Distinct().ToArray();
            int[] ids = await Comandos.Valor<int[]>(conexion, tr, "SELECT array(SELECT id_area FROM public.areas_tematicas WHERE activa AND lower(nombre)=ANY(@nombres))", ct, ("nombres", nombres)) ?? Array.Empty<int>();
            return ids.Length == nombres.Length ? ids : Array.Empty<int>();
        }

        internal static string? TextoOpcional(string? dato)
        {
            return string.IsNullOrWhiteSpace(dato) ? null : dato.Trim();
        }

        internal static string Normalizar(string dato)
        {
            StringBuilder texto = new StringBuilder();
            foreach (char letra in dato.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD))
            {
                if (CharUnicodeInfo.GetUnicodeCategory(letra) != UnicodeCategory.NonSpacingMark)
                {
                    texto.Append(letra);
                }
            }

            return texto.ToString();
        }
    }
}
