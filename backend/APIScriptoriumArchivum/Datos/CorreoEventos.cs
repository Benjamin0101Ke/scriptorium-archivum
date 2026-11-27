using APIScriptoriumArchivum.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace APIScriptoriumArchivum.Datos;
public sealed class CorreoEventos(IDbContextFactory<AppDbContext> db, IConfiguration config, ILogger<CorreoEventos> log, CambiosEnVivo cambios) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        // Permite revisar una restauracion sin enviar correos reales
        if (!config.GetValue("Correo:Habilitado", true))
        {
            return;
        }

        while (!ct.IsCancellationRequested)
        {
            try
            {
                if (await Procesar(ct))
                {
                    continue;
                }
            }
            catch (OperationCanceledException)when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception e)
            {
                log.LogWarning("No se pudo procesar la cola de avisos ({Tipo}).", e.GetType().Name);
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(3), ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task<bool> Procesar(CancellationToken ct)
    {
        await using var c = await db.CreateDbContextAsync(ct);
        Guid reserva = Guid.NewGuid();
        var filas = await Comandos.FilasJson(c, """
            WITH siguiente AS (
                SELECT id_correo FROM public.correos_eventos
                WHERE (estado='Pendiente' AND proximo_intento<=now())
                   OR (estado='Enviando' AND bloqueado_hasta<now())
                ORDER BY id_correo LIMIT 1 FOR UPDATE SKIP LOCKED
            ), reservado AS (
                UPDATE public.correos_eventos m SET estado='Enviando',bloqueado_hasta=now()+interval '1 minute',reserva=@reserva
                FROM siguiente s WHERE m.id_correo=s.id_correo
                RETURNING m.id_correo,m.destino,m.asunto,m.titulo,m.mensaje,m.intentos,m.ciclo
            ) SELECT row_to_json(reservado)::text FROM reservado
            """, ct, ("reserva", reserva));
        if (filas.Count == 0)
        {
            return false;
        }

        var item = filas[0];
        long id = item.GetProperty("id_correo").GetInt64();
        string destino = item.GetProperty("destino").GetString()!;
        string estado;
        string? error = null;
        try
        {
            if (TransporteCorreo.DestinoDePrueba(config, destino))
            {
                estado = "Omitido";
            }
            else
            {
                using var mensaje = PlantillaCorreo.Crear(config["Correo:Remitente"] ?? "no-responder@scriptorium.test", destino, item.GetProperty("asunto").GetString()!, item.GetProperty("titulo").GetString()!, item.GetProperty("mensaje").GetString()!);
                await TransporteCorreo.Enviar(config, mensaje, ct);
                estado = "Enviado";
            }
        }
        catch (OperationCanceledException)when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception e)
        {
            error = e.GetType().Name;
            estado = item.GetProperty("intentos").GetInt32() >= 4 ? "Fallido" : "Pendiente";
            log.LogWarning("No se pudo enviar el aviso {Id} ({Tipo}).", id, error);
        }

        string resultado = estado;
        if (error is not null)
        {
            resultado = "Fallido";
        }

        int ciclo = item.GetProperty("ciclo").GetInt32();
        int numero = item.GetProperty("intentos").GetInt32() + 1;
        await using IDbContextTransaction tr = await c.Database.BeginTransactionAsync(ct);
        int modificados = await Comandos.Ejecutar(c, tr, """
            UPDATE public.correos_eventos SET estado=@estado,intentos=@numero,error_tipo=@error,
                enviado_en=CASE WHEN @estado='Enviado' THEN clock_timestamp() ELSE NULL END,
                proximo_intento=now()+make_interval(mins=>power(2,@numero-1)::int),
                bloqueado_hasta=NULL,reserva=NULL
            WHERE id_correo=@id AND reserva=@reserva AND estado='Enviando' AND ciclo=@ciclo
            """, ct, ("estado", estado), ("numero", numero), ("error", error), ("id", id), ("reserva", reserva), ("ciclo", ciclo));
        if (modificados == 1)
        {
            c.CorreosIntentos.Add(new APIScriptoriumArchivum.Models.CorreosIntentos { IdCorreo = id, Categoria = "Aviso", Ciclo = ciclo, NumeroIntento = numero, Resultado = resultado, ErrorTipo = error });
            await c.SaveChangesAsync(ct);
            await tr.CommitAsync(ct);
            cambios.Publicar("correos");
        }

        return true;
    }
}
