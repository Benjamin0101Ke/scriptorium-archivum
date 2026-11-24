using System.Data;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Npgsql;

namespace APIScriptoriumArchivum.Datos
{
    public class ConsultaPublica
    {
        private NpgsqlDataSource baseDeDatos;
        private ArchivoPdf archivos;
        private static StringComparer OrdenTitulo = StringComparer.Create(CultureInfo.GetCultureInfo("es-SV"), true);

        public ConsultaPublica(NpgsqlDataSource baseDeDatos, ArchivoPdf archivos)
        {
            this.baseDeDatos = baseDeDatos;
            this.archivos = archivos;
        }

        public async Task<List<JsonObject>> Listar(string? q, string? autor, int? anio, string? carrera, string? area,
            string? tipo, string? orden, int? usuario, CancellationToken ct, int? areaAcademica = null)
        {
            string[] terminos = Fichas.Normalizar(q ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Distinct().ToArray();
            int[]? candidatos = null;
            if (terminos.Length > 0 || anio.HasValue || new[] { autor,carrera,area,tipo }.Any(v=>!string.IsNullOrWhiteSpace(v)))
            {
                candidatos=await Candidatos(terminos,autor,anio,carrera,area,tipo,usuario,areaAcademica,ct);
                if(candidatos.Length==0)return [];
            }
            List<FichaPublica> fichas = await Leer(null, usuario, ct, areaAcademica,candidatos);

            IEnumerable<FichaPublica> filtradas = fichas.Where(f =>
                (!anio.HasValue || f.Anio == anio.Value) &&
                Coincide(f.Carrera, carrera) &&
                Coincide(f.Tipo, tipo) &&
                (string.IsNullOrWhiteSpace(autor) || f.Autores.Any(a => CoincideAutor(a, autor))) &&
                (string.IsNullOrWhiteSpace(area) || f.Areas.Any(a => Coincide(a, area))) &&
                terminos.All(p => f.TituloNormal.Contains(p, StringComparison.Ordinal) ||
                    f.AreaNormal.Contains(p, StringComparison.Ordinal) || f.ResumenNormal.Contains(p, StringComparison.Ordinal)));

            IOrderedEnumerable<FichaPublica> ordenadas;
            if (orden == "valoracion")
            {
                ordenadas = filtradas.OrderByDescending(f => f.Datos["valoracion_media"]!.GetValue<decimal>())
                    .ThenByDescending(f => f.Datos["valoraciones_total"]!.GetValue<int>())
                    .ThenBy(f => f.TituloNormal, OrdenTitulo);
            }
            else if (orden == "estrellas")
            {
                ordenadas = filtradas.OrderByDescending(f => f.Datos["estrellas"]!.GetValue<long>())
                    .ThenBy(f => f.TituloNormal, OrdenTitulo);
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
                ordenadas = filtradas.OrderByDescending(f => terminos.Count(p => f.TituloNormal.Contains(p, StringComparison.Ordinal)))
                    .ThenByDescending(f => terminos.Count(p => f.AreaNormal.Contains(p, StringComparison.Ordinal)))
                    .ThenByDescending(f => terminos.Count(p => f.ResumenNormal.Contains(p, StringComparison.Ordinal)))
                    .ThenBy(f => f.TituloNormal, OrdenTitulo);
            }
            return ordenadas.ThenBy(f => f.Id).Select(f => f.Datos).ToList();
        }

        public async Task<FichaPublica> Buscar(int id, CancellationToken ct)
        {
            List<FichaPublica> fichas = await Leer(id, null, ct);
            return fichas.FirstOrDefault() ?? throw new ErrorApi(404, "La ficha no está disponible.");
        }

        // Filter inexpensive metadata first; the final read still validates publication,
        // permissions and the actual PDF, and the existing filters run again on that snapshot.
        private async Task<int[]> Candidatos(string[] terminos,string? autor,int? anio,string? carrera,string? area,string? tipo,
            int? usuario,int? departamento,CancellationToken ct)
        {
            string sql="SELECT t.id_trabajo,t.titulo,t.anio,t.resumen,c.nombre,tt.nombre,p.nombres,a.nombres "+Fichas.RelacionesBasicas+" "+"""
                WHERE t.estado_publicacion='Publicado'
                AND (@anio::int IS NULL OR t.anio=@anio::int)
                AND (@departamento::int IS NULL OR c.id_departamento=@departamento::int)
                AND (@usuario::int IS NULL OR EXISTS(SELECT 1 FROM public.favoritos f
                    WHERE f.id_usuario=@usuario::int AND f.id_trabajo=t.id_trabajo AND f.activo))
                """;
            await using var conexion=await baseDeDatos.OpenConnectionAsync(ct);
            await using var comando=Comandos.Crear(conexion,null,sql,("anio",anio),("departamento",departamento),("usuario",usuario));
            await using var filas=await comando.ExecuteReaderAsync(ct);
            var ids=new List<int>();
            while(await filas.ReadAsync(ct))
            {
                if(!Coincide(filas.GetString(4),carrera)||!Coincide(filas.GetString(5),tipo))continue;
                var autores=filas.GetFieldValue<string[]>(6);var areas=filas.GetFieldValue<string[]>(7);
                if(!string.IsNullOrWhiteSpace(autor)&&!autores.Any(a=>CoincideAutor(a,autor)))continue;
                if(!string.IsNullOrWhiteSpace(area)&&!areas.Any(a=>Coincide(a,area)))continue;
                string titulo=Fichas.Normalizar(filas.GetString(1)),resumen=Fichas.Normalizar(filas.GetString(3)),temas=Fichas.Normalizar(string.Join("; ",areas));
                if(terminos.All(p=>titulo.Contains(p,StringComparison.Ordinal)||resumen.Contains(p,StringComparison.Ordinal)||temas.Contains(p,StringComparison.Ordinal)))ids.Add(filas.GetInt32(0));
            }
            return ids.ToArray();
        }

        private async Task<List<FichaPublica>> Leer(int? id, int? usuario, CancellationToken ct, int? areaAcademica = null,int[]? candidatos=null)
        {
            const string extras = """
                ,'lectura', acceso.lectura, 'descarga', acceso.descarga,
                'estrellas',v.total,'valoracion_media',coalesce(r.promedio,0),'valoraciones_total',r.total,
                'clave_pdf', re.ubicacion, 'nombre_pdf',re.nombre,
                'enlaces',coalesce((SELECT jsonb_agg(jsonb_build_object('id',l.id_recurso::text,'url',l.ubicacion,
                    'tipo',l.tipo_formato,'descripcion',l.descripcion) ORDER BY l.id_recurso)
                    FROM public.recursos_digitales l WHERE l.id_trabajo=t.id_trabajo AND l.clase='ENLACE'
                    AND l.difusion_habilitada AND (l.id_entrega IS NULL OR l.id_entrega=t.id_entrega_final)), '[]'::jsonb)
                """;
            const string condiciones = """
                LEFT JOIN LATERAL (
                    SELECT count(*) AS total
                    FROM public.favoritos WHERE id_trabajo=t.id_trabajo AND activo
                ) v ON true
                LEFT JOIN LATERAL (SELECT avg(estrellas)::numeric AS promedio,count(*)::int AS total
                    FROM public.valoraciones WHERE id_trabajo=t.id_trabajo) r ON true
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
            string sql = "SELECT jsonb_build_object(" + Fichas.DatosBasicos + extras + ")::text " +
                Fichas.RelacionesBasicas + " " + condiciones;

            await using NpgsqlConnection conexion = await baseDeDatos.OpenConnectionAsync(ct);
            await using NpgsqlTransaction tr = await conexion.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);
            List<JsonElement> filas = await Comandos.FilasJson(conexion,tr,sql,ct,("id",id),("usuario",usuario),("area_academica",areaAcademica),("candidatos",candidatos));
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
                fichas.Add(new FichaPublica
                {
                    Id = int.Parse(fila.GetProperty("id").GetString()!),
                    Titulo = fila.GetProperty("titulo").GetString()!,
                    Anio = fila.GetProperty("anio").GetInt32(),
                    Carrera = fila.GetProperty("carrera").GetString()!,
                    Tipo = fila.GetProperty("tipo").GetString()!,
                    Autores = Textos(fila, "autores"),
                    Areas = Textos(fila, "areas"),
                    TituloNormal = Fichas.Normalizar(fila.GetProperty("titulo").GetString()!),
                    AreaNormal = Fichas.Normalizar(fila.GetProperty("area").GetString() ?? ""),
                    ResumenNormal = Fichas.Normalizar(fila.GetProperty("resumen").GetString()!),
                    RutaPdf = ruta,
                    NombrePdf = fila.GetProperty("nombre_pdf").GetString(),
                    Lectura = lectura,
                    Descarga = descarga,
                    Datos = datos
                });
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
            await using NpgsqlConnection conexion = await baseDeDatos.OpenConnectionAsync(ct);
            await using NpgsqlTransaction tr = await conexion.BeginTransactionAsync(ct);
            if (activo)
            {
                if (await Fichas.Bloquear(conexion,tr,id,ct) != "Publicado")
                {
                    throw new ErrorApi(404, "La ficha no está disponible.");
                }
                await Comandos.Ejecutar(conexion,tr, """
                    INSERT INTO public.favoritos(id_usuario,id_trabajo,activo) VALUES (@u,@id,true)
                    ON CONFLICT (id_usuario,id_trabajo) DO UPDATE SET activo=true
                    """,ct,("id",id),("u",usuario));
            }
            else
            {
                await Comandos.Ejecutar(conexion,tr, """
                    UPDATE public.favoritos SET activo=false WHERE id_usuario=@u AND id_trabajo=@id
                    """,ct,("id",id),("u",usuario));
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

        private static bool CoincideAutor(string actual,string? filtro)
        {
            return string.IsNullOrWhiteSpace(filtro)||Fichas.Normalizar(actual).Contains(Fichas.Normalizar(filtro),StringComparison.Ordinal);
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
