using APIScriptoriumArchivum.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Text.Json;
using Npgsql;

namespace APIScriptoriumArchivum.Datos
{
    public class Complementos
    {
        private IDbContextFactory<AppDbContext> baseDeDatos;
        private Avisos avisos;
        public Complementos(IDbContextFactory<AppDbContext> baseDeDatos, Avisos avisos)
        {
            this.baseDeDatos = baseDeDatos;
            this.avisos = avisos;
        }

        public async Task<List<JsonElement>> ListarEnlaces(int id, CancellationToken ct)
        {
            await using AppDbContext conexion = await baseDeDatos.CreateDbContextAsync(ct);
            return await Comandos.FilasJson(conexion, """
                SELECT jsonb_build_object('id',r.id_recurso::text,'url',r.ubicacion,'tipo',r.tipo_formato,
                    'descripcion',r.descripcion,'habilitado',r.difusion_habilitada)::text
                FROM public.recursos_digitales r WHERE r.id_trabajo=@id AND r.clase='ENLACE'
                ORDER BY r.id_recurso
                """, ct, ("id", id));
        }

        public async Task<int> CrearEnlace(int id, DatosEnlace datos, CancellationToken ct, int? actor = null)
        {
            string tipo = datos.Tipo?.Trim() ?? "";
            string descripcion = datos.Descripcion?.Trim() ?? "";
            if (!Uri.TryCreate(datos.Url?.Trim(), UriKind.Absolute, out Uri? url) || url.Scheme != "https" || string.IsNullOrEmpty(url.Host) || !string.IsNullOrEmpty(url.UserInfo) || tipo.Length is 0 or > 100 || descripcion.Length is 0 or > 1500)
            {
                throw new ErrorApi(400, "Indica una URL HTTPS válida, el tipo y la descripción.");
            }

            await using AppDbContext conexion = await baseDeDatos.CreateDbContextAsync(ct);
            await using IDbContextTransaction tr = await conexion.Database.BeginTransactionAsync(ct);
            await Fichas.Bloquear(conexion, tr, id, ct);
            var recurso = new APIScriptoriumArchivum.Models.RecursosDigitales
            {
                IdTrabajo = id,
                Clase = "ENLACE",
                Ubicacion = url.AbsoluteUri,
                TipoFormato = tipo,
                Descripcion = descripcion
            };
            conexion.RecursosDigitales.Add(recurso);
            await conexion.SaveChangesAsync(ct);
            int enlace = recurso.IdRecurso;
            if (actor.HasValue)
            {
                await avisos.Trabajo(conexion, tr, id, actor.Value, "ENLACE_AGREGADO", "Material complementario actualizado", "Biblioteca agregó un enlace complementario al trabajo.", false, ct);
            }

            await tr.CommitAsync(ct);
            return enlace;
        }

        public async Task Habilitar(int id, bool habilitado, CancellationToken ct, int? actor = null)
        {
            await using AppDbContext conexion = await baseDeDatos.CreateDbContextAsync(ct);
            await using IDbContextTransaction tr = await conexion.Database.BeginTransactionAsync(ct);
            int ficha = await Comandos.Valor<int>(conexion, tr, """
                SELECT id_trabajo FROM public.recursos_digitales WHERE id_recurso=@id AND clase='ENLACE'
                """, ct, ("id", id));
            if (ficha == 0)
            {
                throw new ErrorApi(404, "El enlace no existe.");
            }

            await Fichas.Bloquear(conexion, tr, ficha, ct);
            await conexion.RecursosDigitales.Where(r => r.IdRecurso == id).ExecuteUpdateAsync(s => s.SetProperty(r => r.DifusionHabilitada, habilitado), ct);
            if (actor.HasValue)
            {
                await avisos.Trabajo(conexion, tr, ficha, actor.Value, "PERMISO_ENLACE_ACTUALIZADO", "Permiso de enlace actualizado", habilitado ? "Biblioteca habilitó la difusión de un enlace complementario." : "Biblioteca deshabilitó un enlace complementario.", false, ct);
            }

            await tr.CommitAsync(ct);
        }

        public async Task EliminarEnlace(int ficha, int enlace, CancellationToken ct, int? actor = null)
        {
            await using var conexion = await baseDeDatos.CreateDbContextAsync(ct);
            await using var tr = await conexion.Database.BeginTransactionAsync(ct);
            await Fichas.Bloquear(conexion, tr, ficha, ct);
            int eliminados = await conexion.RecursosDigitales.Where(r => r.IdTrabajo == ficha && r.IdRecurso == enlace && r.Clase == "ENLACE").ExecuteDeleteAsync(ct);
            if (eliminados != 1)
            {
                throw new ErrorApi(404, "El enlace no existe.");
            }

            if (actor.HasValue)
            {
                await avisos.Trabajo(conexion, tr, ficha, actor.Value, "ENLACE_ELIMINADO", "Material complementario actualizado", "Biblioteca eliminó un enlace complementario del trabajo.", false, ct);
            }

            await tr.CommitAsync(ct);
        }

        public async Task<List<JsonElement>> Bitacora(CancellationToken ct)
        {
            await using AppDbContext conexion = await baseDeDatos.CreateDbContextAsync(ct);
            return await Comandos.FilasJson(conexion, """
                SELECT jsonb_build_object('id',b.id_evento::text,'nombre',
                    coalesce(nullif(concat_ws(' ',p.nombres,p.apellidos),''), b.actor_tecnico),
                    'fecha',b.fecha_hora,'evento',b.operacion,'detalle',b.detalle)::text
                FROM public.bitacora b LEFT JOIN public.usuarios u ON u.id_usuario=b.id_usuario_actor
                LEFT JOIN public.personas p ON p.id_persona=u.id_persona
                ORDER BY b.fecha_hora DESC,b.id_evento DESC LIMIT 200
                """, ct);
        }

        public async Task<JsonElement?> Guia(CancellationToken ct)
        {
            await using AppDbContext conexion = await baseDeDatos.CreateDbContextAsync(ct);
            List<JsonElement> filas = await Comandos.FilasJson(conexion, """
                SELECT jsonb_build_object('disponible',true,'titulo','Guía de depósito · '||version_procedimiento,
                    'referencia',referencia_documento,'url',CASE WHEN referencia_documento ~* '^https://' THEN referencia_documento END)::text
                FROM public.procedimientos_deposito
                WHERE vigente_desde <= (clock_timestamp() AT TIME ZONE 'America/El_Salvador')::date
                  AND (vigente_hasta IS NULL OR vigente_hasta >= (clock_timestamp() AT TIME ZONE 'America/El_Salvador')::date)
                ORDER BY vigente_desde DESC,id_procedimiento DESC LIMIT 1
                """, ct);
            return filas.Count > 0 ? filas[0] : null;
        }

        public async Task<List<JsonElement>> Reporte(CancellationToken ct)
        {
            await using AppDbContext conexion = await baseDeDatos.CreateDbContextAsync(ct);
            return await Comandos.FilasJson(conexion, """
                SELECT jsonb_build_object('anio',extract(year from e.fecha_deposito)::int,'registradas',count(*),
                    'publicadas',count(*) FILTER (WHERE t.estado_publicacion='Publicado'))::text
                FROM public.trabajos t LEFT JOIN LATERAL (
                    SELECT x.fecha_deposito FROM public.entregas x WHERE x.id_trabajo=t.id_trabajo
                    ORDER BY (x.id_entrega=t.id_entrega_final) DESC NULLS LAST,x.numero_version DESC LIMIT 1
                ) e ON true
                GROUP BY extract(year from e.fecha_deposito)
                ORDER BY extract(year from e.fecha_deposito) DESC NULLS LAST
                """, ct);
        }

        public class DatosEnlace
        {
            public string? Url { get; init; }
            public string? Tipo { get; init; }
            public string? Descripcion { get; init; }
        }

        public class EstadoEnlace
        {
            public bool? Habilitado { get; init; }
        }
    }
}
