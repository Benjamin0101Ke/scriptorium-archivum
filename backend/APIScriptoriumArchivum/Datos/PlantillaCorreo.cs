using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;

namespace APIScriptoriumArchivum.Datos;

public static class PlantillaCorreo
{
    public static MailMessage Crear(string remitente, string destino, string asunto, string titulo,
        string texto, string? codigo = null)
    {
        string E(string valor) => WebUtility.HtmlEncode(valor);
        var mensaje = new MailMessage(new MailAddress(remitente, "Scriptorium Archivum"), new MailAddress(destino))
        {
            Subject = asunto, SubjectEncoding = Encoding.UTF8, BodyEncoding = Encoding.UTF8
        };
        string rutaLogo = Path.Combine(AppContext.BaseDirectory, "Correo", "logo.jpeg");
        string logo = File.Exists(rutaLogo)
            ? "<img src=\"cid:logo-scriptorium\" width=\"72\" height=\"72\" alt=\"Scriptorium Archivum\" style=\"display:block;border-radius:16px;margin:0 auto 16px\">"
            : "";
        string bloqueCodigo = codigo is null ? "" : $"""
            <div style="margin:24px 0;padding:22px;background:#f1edff;border:2px solid #6040c5;border-radius:14px;text-align:center">
              <div style="font-size:12px;color:#534175;margin-bottom:12px">TU CÓDIGO DE VERIFICACIÓN</div>
              <div style="font-size:36px;font-weight:700;letter-spacing:8px;color:#34216b">{E(codigo)}</div>
            </div>
            """;
        string html = $$"""
            <!doctype html><html lang="es"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"></head>
            <body style="margin:0;background:#f2f4fa;font-family:Arial,Helvetica,sans-serif;color:#243353">
            <table role="presentation" width="100%" cellspacing="0" cellpadding="0"><tr><td style="padding:32px 12px" align="center">
            <table role="presentation" width="600" cellspacing="0" cellpadding="0" style="width:100%;max-width:600px;background:#ffffff;border-radius:20px;overflow:hidden">
            <tr><td style="background:#202e51;padding:30px;text-align:center;color:#ffffff;border-bottom:5px solid #06bdca">
              {{logo}}<div style="font-size:25px;font-weight:700">Scriptorium Archivum</div>
              <div style="font-size:13px;color:#d3e8f7;margin-top:8px">Repositorio Digital de Proyectos y Trabajos de Grado</div>
            </td></tr><tr><td style="padding:32px">
              <h1 style="margin:0 0 20px;font-size:24px;line-height:1.3;color:#38236e">{{E(titulo)}}</h1>
              {{bloqueCodigo}}
              <div style="font-size:16px;line-height:1.8">{{E(texto).Replace("\n", "<br>")}}</div>
              <p style="margin:26px 0 0;padding-top:20px;border-top:1px solid #e0e5f0;font-size:13px;line-height:1.6;color:#5b6783">Consultá los detalles ingresando al repositorio desde tu dirección habitual.</p>
            </td></tr><tr><td style="padding:20px;background:#f8f6ff;border-top:3px solid #ffbf36;text-align:center;font-size:12px;color:#61708c">
              Universidad de El Salvador · Facultad Multidisciplinaria Oriental<br>Mensaje automático de Scriptorium Archivum.
            </td></tr></table></td></tr></table></body></html>
            """;
        mensaje.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(
            titulo + "\n\n" + (codigo is null ? "" : "Código: " + codigo + "\n\n") + texto, Encoding.UTF8, MediaTypeNames.Text.Plain));
        var vista = AlternateView.CreateAlternateViewFromString(html, Encoding.UTF8, MediaTypeNames.Text.Html);
        if (File.Exists(rutaLogo))
            vista.LinkedResources.Add(new LinkedResource(rutaLogo, "image/jpeg")
            { ContentId = "logo-scriptorium", TransferEncoding = TransferEncoding.Base64 });
        mensaje.AlternateViews.Add(vista);
        return mensaje;
    }
}
