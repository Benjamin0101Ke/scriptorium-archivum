using APIScriptoriumArchivum.Datos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APIScriptoriumArchivum.Controllers;

[ApiController, Authorize]
[Route("api/presencia")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class PresenciaController(Presencia presencia) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Marcar(Actividad datos, CancellationToken ct)
    {
        if (datos.Sesion == Guid.Empty) return BadRequest(new { error = "Identificador de sesión inválido." });
        await presencia.Marcar((Cuenta)HttpContext.Items[typeof(Cuenta)]!, datos.Sesion, ct);
        return Ok(new { ok = true });
    }

    [Authorize(Roles = "Administrador")]
    [HttpGet]
    public async Task<IActionResult> Conectados(CancellationToken ct)
    {
        var usuarios = await presencia.Conectados(ct);
        return Ok(new { total = usuarios.Count, usuarios, ventana_segundos = 120 });
    }

    public record Actividad(Guid Sesion);
}
