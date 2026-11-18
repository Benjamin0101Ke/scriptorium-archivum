using System.Globalization;
using System.Threading.Channels;

namespace APIScriptoriumArchivum.Datos;

public sealed class CorreoRecuperacion(IConfiguration config, ILogger<CorreoRecuperacion> log) : BackgroundService
{
    private readonly Channel<Mensaje> cola = Channel.CreateBounded<Mensaje>(new BoundedChannelOptions(64)
    {
        SingleReader = true
    });

    public void Encolar(string destino, string codigo, int minutos)
    {
        if (!cola.Writer.TryWrite(new(destino, codigo, minutos, DateTimeOffset.UtcNow.AddMinutes(minutos))))
            log.LogWarning("No hay espacio para más mensajes de recuperación.");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (Mensaje item in cola.Reader.ReadAllAsync(stoppingToken))
        {
            if (item.Vence <= DateTimeOffset.UtcNow) continue;
            try
            {
                if (TransporteCorreo.DestinoDePrueba(config, item.Destino)) continue;
                using var mensaje = PlantillaCorreo.Crear(
                    config["Correo:Remitente"] ?? "no-responder@scriptorium.test", item.Destino,
                    "Código de recuperación · Scriptorium Archivum", "Recuperá el acceso a tu cuenta",
                    $"Este código vence en {item.Minutos.ToString(CultureInfo.InvariantCulture)} minutos y se puede usar una sola vez.\nIngresalo en la pantalla de recuperación para elegir tu nueva contraseña.\n\nSi no solicitaste este cambio, ignorá este mensaje. No compartás el código con nadie.", item.Codigo);
                await TransporteCorreo.Enviar(config, mensaje, stoppingToken);
            }
            catch (Exception e) when (!stoppingToken.IsCancellationRequested)
            {
                log.LogWarning("No se pudo enviar un correo de recuperación ({Tipo}).", e.GetType().Name);
            }
        }
    }

    private sealed record Mensaje(string Destino, string Codigo, int Minutos, DateTimeOffset Vence);
}
