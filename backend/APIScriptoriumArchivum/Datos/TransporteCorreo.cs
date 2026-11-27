using System.Net;
using System.Net.Mail;

namespace APIScriptoriumArchivum.Datos;
public static class TransporteCorreo
{
    public static bool DestinoDePrueba(IConfiguration config, string destino)
    {
        return (destino.EndsWith(".test", StringComparison.OrdinalIgnoreCase) || destino.EndsWith(".invalid", StringComparison.OrdinalIgnoreCase)) && (config["Correo:Host"] ?? "").ToLowerInvariant()is not ("localhost" or "127.0.0.1");
    }

    public static async Task Enviar(IConfiguration config, MailMessage mensaje, CancellationToken ct)
    {
        string? host = config["Correo:Host"];
        if (string.IsNullOrWhiteSpace(host))
        {
            throw new InvalidOperationException("Falta configurar el servidor SMTP");
        }
        using var smtp = new SmtpClient(host, config.GetValue("Correo:Puerto", 587))
        {
            EnableSsl = config.GetValue("Correo:Tls", true)
        };
        string? usuario = config["Correo:Usuario"];
        if (!string.IsNullOrEmpty(usuario))
        {
            if (!smtp.EnableSsl)
            {
                throw new InvalidOperationException("SMTP con credenciales requiere TLS.");
            }

            smtp.Credentials = new NetworkCredential(usuario, config["Correo:Password"]);
        }

        using var limite = CancellationTokenSource.CreateLinkedTokenSource(ct);
        limite.CancelAfter(TimeSpan.FromSeconds(15));
        await smtp.SendMailAsync(mensaje, limite.Token);
    }
}
