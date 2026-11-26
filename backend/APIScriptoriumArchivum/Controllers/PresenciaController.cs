using APIScriptoriumArchivum.Datos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APIScriptoriumArchivum.Controllers;
// Registra actividad de sesiones y permite a Administracion consultar usuarios conectados
[ApiController, Authorize]
[Route("api/presencia")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class PresenciaController(Presencia presencia) : ControllerBase
{
    // Actualiza la ultima actividad de la sesion indicada
    [HttpPost]
    public async Task<IActionResult> Marcar(Actividad datos, CancellationToken ct)
    {
        if (datos.Sesion == Guid.Empty)
        {
            return BadRequest(new { error = "Identificador de sesión inválido." });
        }

        await presencia.Marcar((Cuenta)HttpContext.Items[typeof(Cuenta)]!, datos.Sesion, ct);
        return Ok(new { ok = true });
    }

    // Consulta las cuentas cuya actividad sigue dentro de la ventana de presencia
    [Authorize(Roles = "Administrador")]
    [HttpGet]
    public async Task<IActionResult> Conectados(CancellationToken ct)
    {
        var usuarios = await presencia.Conectados(ct);
        return Ok(new { total = usuarios.Count, usuarios, ventana_segundos = 120 });
    }

    // Identificador de la sesion cuya actividad se va a actualizar
    public record Actividad(Guid Sesion);
}
