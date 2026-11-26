using APIScriptoriumArchivum.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Text.Json;
using Npgsql;

namespace APIScriptoriumArchivum.Datos
{
    public sealed class RegistroCorreos
    {
        private IDbContextFactory<AppDbContext> db;
        public RegistroCorreos(IDbContextFactory<AppDbContext> db)
        {
            this.db = db;
        }

        public async Task Reenviar(long id, int actor, CancellationToken ct)
        {
            await using AppDbContext c = await db.CreateDbContextAsync(ct);
            await using IDbContextTransaction tr = await c.Database.BeginTransactionAsync(ct);
            var correos = await c.CorreosEventos.FromSqlRaw("SELECT * FROM public.correos_eventos WHERE id_correo=@id FOR UPDATE", Comandos.Parametros(("id", id))).ToListAsync(ct);
            var correo = correos.SingleOrDefault();
            if (correo is null)
            {
                throw new ErrorApi(404, "El correo no existe");
            }

            if (correo.Estado != "Fallido")
            {
                throw new ErrorApi(409, "Solo se pueden reenviar avisos fallidos");
            }

            correo.Estado = "Pendiente";
            correo.Intentos = 0;
            correo.Ciclo++;
            correo.ProximoIntento = DateTime.UtcNow;
            correo.BloqueadoHasta = null;
            correo.Reserva = null;
            correo.ErrorTipo = null;
            correo.EnviadoEn = null;
            c.Bitacora.Add(new APIScriptoriumArchivum.Models.Bitacora { IdUsuarioActor = actor, Operacion = "REENVIAR_CORREO", TablaAfectada = "correos_eventos", RegistroAfectado = id.ToString(), Detalle = "Reenvio de aviso fallido solicitado" });
            await c.SaveChangesAsync(ct);
            await tr.CommitAsync(ct);
        }

        public async Task<List<JsonElement>> Intentos(long id, CancellationToken ct)
        {
            await using AppDbContext c = await db.CreateDbContextAsync(ct);
            bool existe = await Comandos.Valor<bool>(c, null, "SELECT EXISTS(SELECT 1 FROM public.correos_eventos WHERE id_correo=@id)", ct, ("id", id));
            if (!existe)
            {
                throw new ErrorApi(404, "El correo no existe");
            }

            return await Comandos.FilasJson(c, """
                SELECT jsonb_build_object('id',id_intento::text,'ciclo',ciclo,'numero_intento',numero_intento,
                    'resultado',resultado,'fecha',fecha,'error_tipo',error_tipo)::text
                FROM public.correos_intentos WHERE id_correo=@id
                ORDER BY id_intento DESC LIMIT 200
                """, ct, ("id", id));
        }

        public async Task<List<JsonElement>> Recuperaciones(CancellationToken ct)
        {
            await using AppDbContext c = await db.CreateDbContextAsync(ct);
            return await Comandos.FilasJson(c, """
                SELECT jsonb_build_object('id',id_intento::text,'id_usuario',id_usuario,
                    'numero_intento',numero_intento,'resultado',resultado,'fecha',fecha,'error_tipo',error_tipo)::text
                FROM public.correos_intentos WHERE categoria='Recuperacion'
                ORDER BY id_intento DESC LIMIT 100
                """, ct);
        }

        public async Task Recuperacion(int usuario, int intento, string resultado, string? error, CancellationToken ct)
        {
            await using AppDbContext c = await db.CreateDbContextAsync(ct);
            c.CorreosIntentos.Add(new APIScriptoriumArchivum.Models.CorreosIntentos { IdUsuario = usuario, Categoria = "Recuperacion", NumeroIntento = intento, Resultado = resultado, ErrorTipo = error });
            await c.SaveChangesAsync(ct);
        }
    }
}
