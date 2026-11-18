using Npgsql;

namespace APIScriptoriumArchivum.Datos;

public sealed class CorreoEventos(NpgsqlDataSource db, IConfiguration config, ILogger<CorreoEventos> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                if (await Procesar(ct)) continue;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
            catch (Exception e) { log.LogWarning("No se pudo procesar la cola de avisos ({Tipo}).", e.GetType().Name); }
            try { await Task.Delay(TimeSpan.FromSeconds(3), ct); }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task<bool> Procesar(CancellationToken ct)
    {
        await using var c = await db.OpenConnectionAsync(ct);
        var filas = await Comandos.FilasJson(c, """
            WITH siguiente AS (
                SELECT id_correo FROM public.correos_eventos
                WHERE (estado='Pendiente' AND proximo_intento<=now())
                   OR (estado='Enviando' AND bloqueado_hasta<now())
                ORDER BY id_correo LIMIT 1 FOR UPDATE SKIP LOCKED
            ), reservado AS (
                UPDATE public.correos_eventos m SET estado='Enviando',bloqueado_hasta=now()+interval '1 minute'
                FROM siguiente s WHERE m.id_correo=s.id_correo
                RETURNING m.id_correo,m.destino,m.asunto,m.titulo,m.mensaje,m.intentos
            ) SELECT row_to_json(reservado)::text FROM reservado
            """, ct);
        if (filas.Count == 0) return false;
        var item = filas[0];
        long id = item.GetProperty("id_correo").GetInt64();
        string destino = item.GetProperty("destino").GetString()!;
        string estado;
        string? error = null;
        try
        {
            if (TransporteCorreo.DestinoDePrueba(config, destino)) estado = "Omitido";
            else
            {
                using var mensaje = PlantillaCorreo.Crear(config["Correo:Remitente"] ?? "no-responder@scriptorium.test", destino,
                    item.GetProperty("asunto").GetString()!, item.GetProperty("titulo").GetString()!, item.GetProperty("mensaje").GetString()!);
                await TransporteCorreo.Enviar(config, mensaje, ct);
                estado = "Enviado";
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception e)
        {
            error = e.GetType().Name;
            estado = item.GetProperty("intentos").GetInt32() >= 4 ? "Fallido" : "Pendiente";
            log.LogWarning("No se pudo enviar el aviso {Id} ({Tipo}).", id, error);
        }
        await using var actualizar = Comandos.Crear(c, null, """
            UPDATE public.correos_eventos SET estado=@estado,intentos=intentos+1,error_tipo=@error,
                enviado_en=CASE WHEN @estado='Enviado' THEN clock_timestamp() ELSE NULL END,
                proximo_intento=now()+make_interval(mins=>LEAST(30, power(2,intentos)::int)),bloqueado_hasta=NULL
            WHERE id_correo=@id
            """, ("estado", estado), ("error", error), ("id", id));
        await actualizar.ExecuteNonQueryAsync(ct);
        return true;
    }
}
