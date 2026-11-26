using System.Net.Mail;
using APIScriptoriumArchivum.Datos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace APIScriptoriumArchivum.Controllers;
// Atiende la solicitud y confirmacion del codigo de recuperacion de contrasena
[ApiController]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[EnableRateLimiting("recuperacion")]
[Route("api/auth/[controller]")]
public sealed class RecuperacionController(Recuperacion recuperacion) : ControllerBase
{
    // Solicita el codigo sin revelar si el correo pertenece a una cuenta
    [HttpPost("solicitar")]
    public async Task<IActionResult> Solicitar(Solicitud datos, CancellationToken ct)
    {
        string? correo = ValidarCorreo(datos.Correo);
        if (correo is null)
        {
            return BadRequest(new { error = "Ingresa un correo válido." });
        }

        await recuperacion.Solicitar(correo, ct, HttpContext.TraceIdentifier);
        return Accepted(new { mensaje = "Si el correo corresponde a una cuenta habilitada y no se alcanzó el límite de envíos, recibirás un código.", espera_segundos = 60, solicitud_id = HttpContext.TraceIdentifier });
    }

    // Valida el codigo y guarda la nueva contrasena si todavia puede utilizarse
    [HttpPost("confirmar")]
    public async Task<IActionResult> Confirmar(Confirmacion datos, CancellationToken ct)
    {
        string? correo = ValidarCorreo(datos.Correo);
        string codigo = datos.Codigo?.Trim() ?? "";
        if (correo is null || codigo.Length != 6 || codigo.Any(c => !char.IsAsciiDigit(c)))
        {
            return BadRequest(new { error = "Ingresa el correo y el código de seis dígitos." });
        }

        if (datos.Nueva is not { Length: >= 8 and <= 512 })
        {
            return BadRequest(new { error = "Usa una contraseña de entre 8 y 512 caracteres." });
        }

        if (!await recuperacion.Confirmar(correo, codigo, datos.Nueva, ct))
        {
            return BadRequest(new { error = "El código es incorrecto, venció o ya fue utilizado." });
        }

        return Ok(new { ok = true });
    }

    // Comprueba el formato del correo y normaliza sus espacios y mayusculas
    private static string? ValidarCorreo(string? valor)
    {
        string correo = valor?.Trim().ToLowerInvariant() ?? "";
        return correo.Length <= 150 && MailAddress.TryCreate(correo, out var direccion) && direccion.Address == correo ? correo : null;
    }

    // Correo de la cuenta para la que se solicita un codigo de recuperacion
    public record Solicitud(string? Correo);
    // Correo, codigo recibido y nueva contrasena para recuperar el acceso
    public record Confirmacion(string? Correo, string? Codigo, string? Nueva);
}
