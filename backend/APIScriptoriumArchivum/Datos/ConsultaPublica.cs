using APIScriptoriumArchivum.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Npgsql;

namespace APIScriptoriumArchivum.Datos
{
    public class ConsultaPublica
    {
        private IDbContextFactory<AppDbContext> baseDeDatos;
        private ArchivoPdf archivos;
        private static StringComparer OrdenTitulo = StringComparer.Create(CultureInfo.GetCultureInfo("es-SV"), true);
        public ConsultaPublica(IDbContextFactory<AppDbContext> baseDeDatos, ArchivoPdf archivos)
        {
            this.baseDeDatos = baseDeDatos;
            this.archivos = archivos;
        }

        public async Task<List<JsonObject>> Listar(string? q, string? autor, int? anio, string? carrera, string? area, string? tipo, string? orden, int? usuario, CancellationToken ct, int? areaAcademica = null)
        {
            string[] terminos = Fichas.Normalizar(q ?? "").Split((char[]? )null, StringSplitOptions.RemoveEmptyEntries).Distinct().ToArray();
            int[]? candidatos = null;
            if (terminos.Length > 0 || anio.HasValue || new[]
            {
                autor,
                carrera,
                area,
                tipo
            }.Any(v => !string.IsNullOrWhiteSpace(v)))
            {
                candidatos = await Candidatos(terminos, autor, anio, carrera, area, tipo, usuario, areaAcademica, ct);
                if (candidatos.Length == 0)
                {
                    return[];
                }
            }

            List<FichaPublica> fichas = await Leer(null, usuario, ct, areaAcademica, candidatos);
            IEnumerable<FichaPublica> filtradas = fichas.Where(f => (!anio.HasValue || f.Anio == anio.Value) && Coincide(f.Carrera, carrera) && Coincide(f.Tipo, tipo) && (string.IsNullOrWhiteSpace(autor) || f.Autores.Any(a => CoincideAutor(a, autor))) && (string.IsNullOrWhiteSpace(area) || f.Areas.Any(a => Coincide(a, area))) && terminos.All(p => f.TituloNormal.Contains(p, StringComparison.Ordinal) || f.AreaNormal.Contains(p, StringComparison.Ordinal) || f.ResumenNormal.Contains(p, StringComparison.Ordinal)));
            IOrderedEnumerable<FichaPublica> ordenadas;
            if (orden == "estrellas")
            {
                ordenadas = filtradas.OrderByDescending(f => f.Datos["estrellas"]!.GetValue<long>()).ThenBy(f => f.TituloNormal, OrdenTitulo);
            }
            else if (orden == "anio")
            {
                ordenadas = filtradas.OrderByDescending(f => f.Anio).ThenBy(f => f.TituloNormal, OrdenTitulo);
            }
            else if (orden == "titulo")
            {
                ordenadas = filtradas.OrderBy(f => f.TituloNormal, OrdenTitulo);
            }
            else
            {
                ordenadas = filtradas.OrderByDescending(f => terminos.Count(p => f.TituloNormal.Contains(p, StringComparison.Ordinal))).ThenByDescending(f => terminos.Count(p => f.AreaNormal.Contains(p, StringComparison.Ordinal))).ThenByDescending(f => terminos.Count(p => f.ResumenNormal.Contains(p, StringComparison.Ordinal))).ThenBy(f => f.TituloNormal, OrdenTitulo);
            }

            return ordenadas.ThenBy(f => f.Id).Select(f => f.Datos).ToList();
        }

        public async Task<FichaPublica> Buscar(int id, CancellationToken ct)
        {
            List<FichaPublica> fichas = await Leer(id, null, ct);
            return fichas.FirstOrDefault() ?? throw new ErrorApi(404, "La ficha no está disponible.");
        }

        // Filtra los metadatos antes de comprobar los permisos y el archivo
        private async Task<int[]> Candidatos(string[] terminos, string? autor, int? anio, string? carrera, string? area, string? tipo, int? usuario, int? departamento, CancellationToken ct)
        {
            string sql = "SELECT t.id_trabajo,t.titulo,t.anio,t.resumen,c.nombre AS carrera,tt.nombre AS tipo,p.nombres AS autores,a.nombres AS areas " + Fichas.RelacionesBasicas + " " + """
                WHERE t.estado_publicacion='Publicado'
                AND (@anio::int IS NULL OR t.anio=@anio::int)
                AND (@departamento::int IS NULL OR c.id_departamento=@departamento::int)
                AND (@usuario::int IS NULL OR EXISTS(SELECT 1 FROM public.favoritos f
                    WHERE f.id_usuario=@usuario::int AND f.id_trabajo=t.id_trabajo AND f.activo))
                """;
            await using var conexion = await baseDeDatos.CreateDbContextAsync(ct);
            var filas = await Comandos.Filas<CandidatoFicha>(conexion, sql, ct, ("anio", anio), ("departamento", departamento), ("usuario", usuario));
            var ids = new List<int>();
            foreach (var fila in filas)
            {
                if (!Coincide(fila.Carrera, carrera) || !Coincide(fila.Tipo, tipo))
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(autor) && !fila.Autores.Any(a => CoincideAutor(a, autor)))
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(area) && !fila.Areas.Any(a => Coincide(a, area)))
                {
                    continue;
                }

                string titulo = Fichas.Normalizar(fila.Titulo);
                string resumen = Fichas.Normalizar(fila.Resumen);
                string temas = Fichas.Normalizar(string.Join("; ", fila.Areas));
                if (terminos.All(p => titulo.Contains(p, StringComparison.Ordinal) || resumen.Contains(p, StringComparison.Ordinal) || temas.Contains(p, StringComparison.Ordinal)))
                {
                    ids.Add(fila.IdTrabajo);
                }
            }

            return ids.ToArray();
        }

        private async Task<List<FichaPublica>> Leer(int? id, int? usuario, CancellationToken ct, int? areaAcademica = null, int[]? candidatos = null)
        {
            const string extras = """
                ,'lectura', acceso.lectura, 'descarga', acceso.descarga,
                'estrellas',coalesce(v.estrellas,0),
                'clave_pdf', re.ubicacion, 'nombre_pdf',re.nombre,
                'enlaces',coalesce((SELECT jsonb_agg(jsonb_build_object('id',l.id_recurso::text,'url',l.ubicacion,
                    'tipo',l.tipo_formato,'descripcion',l.descripcion) ORDER BY l.id_recurso)
                    FROM public.recursos_digitales l WHERE l.id_trabajo=t.id_trabajo AND l.clase='ENLACE'
                    AND l.difusion_habilitada AND (l.id_entrega IS NULL OR l.id_entrega=t.id_entrega_final)), '[]'::jsonb)
                """;
            const string condiciones = """
                LEFT JOIN public.vw_estrellas_por_trabajo v ON v.id_trabajo=t.id_trabajo
                LEFT JOIN public.recursos_digitales re ON re.id_trabajo=t.id_trabajo AND re.id_entrega=t.id_entrega_final
                      AND re.clase='PDF' AND re.estado_carga='Disponible'
                CROSS JOIN LATERAL public.fn_elegibilidad_difusion(t.id_trabajo,re.id_recurso IS NOT NULL) acceso
                WHERE t.estado_publicacion='Publicado' AND acceso.ficha_publica
                AND (@id::int IS NULL OR @id::int=t.id_trabajo)
                AND (@candidatos::int[] IS NULL OR t.id_trabajo=ANY(@candidatos::int[]))
                AND (@area_academica::int IS NULL OR EXISTS(SELECT 1 FROM public.carreras ca
                    WHERE ca.id_carrera=t.id_carrera AND ca.id_departamento=@area_academica))
                AND (@usuario::int IS NULL OR EXISTS (SELECT 1 FROM public.favoritos f
                    WHERE f.id_usuario=@usuario::int AND f.id_trabajo=t.id_trabajo AND f.activo))
                """;
            string sql = "SELECT jsonb_build_object(" + Fichas.DatosBasicos + extras + ")::text " + Fichas.RelacionesBasicas + " " + condiciones;
            await using AppDbContext conexion = await baseDeDatos.CreateDbContextAsync(ct);
            await using IDbContextTransaction tr = await conexion.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);
            List<JsonElement> filas = await Comandos.FilasJson(conexion, tr, sql, ct, ("id", id), ("usuario", usuario), ("area_academica", areaAcademica), ("candidatos", candidatos));
            List<FichaPublica> fichas = new List<FichaPublica>();
            foreach (JsonElement fila in filas)
            {
                bool lectura = fila.GetProperty("lectura").GetBoolean();
                bool descarga = fila.GetProperty("descarga").GetBoolean();
                string? ruta = archivos.Ubicar(fila.GetProperty("clave_pdf").GetString());
                if ((lectura || descarga) && ruta is null)
                {
                    continue;
                }

                JsonObject datos = JsonNode.Parse(fila.GetRawText())!.AsObject();
                datos.Remove("clave_pdf");
                datos.Remove("nombre_pdf");
                datos.Remove("autor_ids");
                datos.Remove("area_ids");
                datos.Remove("id_carrera");
                datos.Remove("id_tipo_trabajo");
                fichas.Add(new FichaPublica { Id = int.Parse(fila.GetProperty("id").GetString()!), Titulo = fila.GetProperty("titulo").GetString()!, Anio = fila.GetProperty("anio").GetInt32(), Carrera = fila.GetProperty("carrera").GetString()!, Tipo = fila.GetProperty("tipo").GetString()!, Autores = Textos(fila, "autores"), Areas = Textos(fila, "areas"), TituloNormal = Fichas.Normalizar(fila.GetProperty("titulo").GetString()!), AreaNormal = Fichas.Normalizar(fila.GetProperty("area").GetString() ?? ""), ResumenNormal = Fichas.Normalizar(fila.GetProperty("resumen").GetString()!), RutaPdf = ruta, NombrePdf = fila.GetProperty("nombre_pdf").GetString(), Lectura = lectura, Descarga = descarga, Datos = datos });
            }

            await tr.CommitAsync(ct);
            return fichas;
        }

        public async Task Favorito(int id, int usuario, bool activo, CancellationToken ct)
        {
            if (activo)
            {
                await Buscar(id, ct);
            }

            await using AppDbContext conexion = await baseDeDatos.CreateDbContextAsync(ct);
            await using IDbContextTransaction tr = await conexion.Database.BeginTransactionAsync(ct);
            if (activo)
            {
                if (await Fichas.Bloquear(conexion, tr, id, ct) != "Publicado")
                {
                    throw new ErrorApi(404, "La ficha no está disponible.");
                }

                var favorito = await conexion.Favoritos.SingleOrDefaultAsync(f => f.IdUsuario == usuario && f.IdTrabajo == id, ct);
                if (favorito is null)
                {
                    favorito = new APIScriptoriumArchivum.Models.Favoritos
                    {
                        IdUsuario = usuario,
                        IdTrabajo = id,
                        Activo = true
                    };
                    conexion.Favoritos.Add(favorito);
                }
                else
                {
                    favorito.Activo = true;
                }

                await conexion.SaveChangesAsync(ct);
            }
            else
            {
                await conexion.Favoritos.Where(f => f.IdUsuario == usuario && f.IdTrabajo == id).ExecuteUpdateAsync(s => s.SetProperty(f => f.Activo, false), ct);
            }

            await tr.CommitAsync(ct);
        }

        private static string[] Textos(JsonElement fila, string nombre)
        {
            return fila.GetProperty(nombre).EnumerateArray().Select(x => x.GetString() ?? "").ToArray();
        }

        private static bool Coincide(string actual, string? filtro)
        {
            return string.IsNullOrWhiteSpace(filtro) || Fichas.Normalizar(actual) == Fichas.Normalizar(filtro);
        }

        private static bool CoincideAutor(string actual, string? filtro)
        {
            return string.IsNullOrWhiteSpace(filtro) || Fichas.Normalizar(actual).Contains(Fichas.Normalizar(filtro), StringComparison.Ordinal);
        }

        public class FichaPublica
        {
            public int Id { get; init; }
            public string Titulo { get; init; } = "";
            public int Anio { get; init; }
            public string Carrera { get; init; } = "";
            public string Tipo { get; init; } = "";
            public string[] Autores { get; init; } = Array.Empty<string>();
            public string[] Areas { get; init; } = Array.Empty<string>();
            public string TituloNormal { get; init; } = "";
            public string AreaNormal { get; init; } = "";
            public string ResumenNormal { get; init; } = "";
            public string? RutaPdf { get; init; }
            public string? NombrePdf { get; init; }
            public bool Lectura { get; init; }
            public bool Descarga { get; init; }
            public JsonObject Datos { get; init; } = new JsonObject();
        }
    }
}
