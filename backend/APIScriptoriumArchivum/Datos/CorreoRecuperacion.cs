using System.Globalization;
using System.Threading.Channels;

namespace APIScriptoriumArchivum.Datos;

public sealed class CorreoRecuperacion(IConfiguration config, ILogger<CorreoRecuperacion> log, RegistroCorreos registro) : BackgroundService
{
    private Channel<Mensaje> cola = Channel.CreateBounded<Mensaje>(new BoundedChannelOptions(64)
    {
        SingleReader = true
    });

    public void Encolar(int usuario, string destino, string codigo, int minutos)
    {
        if (!cola.Writer.TryWrite(new(usuario, destino, codigo, minutos, DateTimeOffset.UtcNow.AddMinutes(minutos))))
        {
            log.LogWarning("No hay espacio para más mensajes de recuperación.");
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (Mensaje item in cola.Reader.ReadAllAsync(stoppingToken))
        {
            for (int intento = 1; intento <= 5 && item.Vence > DateTimeOffset.UtcNow; intento++)
            {
                string resultado = "Enviado";
                string? error = null;
                try
                {
                    if (TransporteCorreo.DestinoDePrueba(config, item.Destino))
                    {
                        resultado = "Omitido";
                    }
                    else
                    {
                        int minutos = Math.Max(1, (int)Math.Ceiling((item.Vence - DateTimeOffset.UtcNow).TotalMinutes));
                        using var mensaje = PlantillaCorreo.Crear(
                            config["Correo:Remitente"] ?? "no-responder@scriptorium.test", item.Destino,
                            "Código de recuperación · Scriptorium Archivum", "Recuperá el acceso a tu cuenta",
                            $"Este código vence en {minutos.ToString(CultureInfo.InvariantCulture)} minutos y se puede usar una sola vez.\nIngresalo en la pantalla de recuperación para elegir tu nueva contraseña.\n\nSi no solicitaste este cambio, ignorá este mensaje. No compartás el código con nadie.", item.Codigo);
                        await TransporteCorreo.Enviar(config, mensaje, stoppingToken);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception e)
                {
                    resultado = "Fallido";
                    error = e.GetType().Name;
                    log.LogWarning("No se pudo enviar un correo de recuperacion, intento {Intento} ({Tipo})", intento, error);
                }
                try
                {
                    await registro.Recuperacion(item.Usuario, intento, resultado, error, stoppingToken);
                }
                catch (Exception e) when (!stoppingToken.IsCancellationRequested)
                {
                    log.LogWarning("No se pudo registrar el intento de recuperacion ({Tipo})", e.GetType().Name);
                }
                if (resultado != "Fallido" || intento == 5)
                {
                    break;
                }
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, intento)), stoppingToken);
            }
        }
    }

    private sealed class Mensaje
    {
        public int Usuario { get; }
        public string Destino { get; }
        public string Codigo { get; }
        public int Minutos { get; }
        public DateTimeOffset Vence { get; }

        public Mensaje(int usuario, string destino, string codigo, int minutos, DateTimeOffset vence)
        {
            Usuario = usuario;
            Destino = destino;
            Codigo = codigo;
            Minutos = minutos;
            Vence = vence;
        }
    }
}
