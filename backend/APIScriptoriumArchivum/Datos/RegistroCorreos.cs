using System.Text.Json;
using Npgsql;

namespace APIScriptoriumArchivum.Datos
{
    public sealed class RegistroCorreos
    {
        private NpgsqlDataSource db;

        public RegistroCorreos(NpgsqlDataSource db)
        {
            this.db = db;
        }

        public async Task Reenviar(long id, int actor, CancellationToken ct)
        {
            await using NpgsqlConnection c = await db.OpenConnectionAsync(ct);
            await using NpgsqlTransaction tr = await c.BeginTransactionAsync(ct);
            string? estado = await Comandos.Valor<string>(c, tr,
                "SELECT estado FROM public.correos_eventos WHERE id_correo=@id FOR UPDATE", ct, ("id", id));
            if (estado is null)
            {
                throw new ErrorApi(404, "El correo no existe");
            }
            if (estado != "Fallido")
            {
                throw new ErrorApi(409, "Solo se pueden reenviar avisos fallidos");
            }
            await Comandos.Ejecutar(c, tr, """
                UPDATE public.correos_eventos SET estado='Pendiente',intentos=0,ciclo=ciclo+1,
                    proximo_intento=clock_timestamp(),bloqueado_hasta=NULL,reserva=NULL,error_tipo=NULL,enviado_en=NULL
                WHERE id_correo=@id;
                INSERT INTO public.bitacora(id_usuario_actor,operacion,tabla_afectada,registro_afectado,detalle)
                VALUES(@actor,'REENVIAR_CORREO','correos_eventos',@id::text,'Reenvio de aviso fallido solicitado');
                """, ct, ("id", id), ("actor", actor));
            await tr.CommitAsync(ct);
        }

        public async Task<List<JsonElement>> Intentos(long id, CancellationToken ct)
        {
            await using NpgsqlConnection c = await db.OpenConnectionAsync(ct);
            bool existe = await Comandos.Valor<bool>(c, null,
                "SELECT EXISTS(SELECT 1 FROM public.correos_eventos WHERE id_correo=@id)", ct, ("id", id));
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
            await using NpgsqlConnection c = await db.OpenConnectionAsync(ct);
            return await Comandos.FilasJson(c, """
                SELECT jsonb_build_object('id',id_intento::text,'id_usuario',id_usuario,
                    'numero_intento',numero_intento,'resultado',resultado,'fecha',fecha,'error_tipo',error_tipo)::text
                FROM public.correos_intentos WHERE categoria='Recuperacion'
                ORDER BY id_intento DESC LIMIT 100
                """, ct);
        }

        public async Task Recuperacion(int usuario, int intento, string resultado, string? error, CancellationToken ct)
        {
            await using NpgsqlConnection c = await db.OpenConnectionAsync(ct);
            await using NpgsqlCommand comando = Comandos.Crear(c, null, """
                INSERT INTO public.correos_intentos(id_usuario,categoria,numero_intento,resultado,error_tipo)
                VALUES(@usuario,'Recuperacion',@intento,@resultado,@error)
                """, ("usuario", usuario), ("intento", intento), ("resultado", resultado), ("error", error));
            await comando.ExecuteNonQueryAsync(ct);
        }
    }
}
