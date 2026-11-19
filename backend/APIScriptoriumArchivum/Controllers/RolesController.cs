using System.Security.Claims;
using APIScriptoriumArchivum.Datos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APIScriptoriumArchivum.Controllers;

[ApiController]
[Authorize(Roles = "Administrador")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("api/admin/roles")]
public sealed class RolesController(GestionRoles gestion, Complementos complementos, Recuperacion recuperacion, AsignacionesDocentes asignaciones, RegistroCorreos registroCorreos) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] string? q, [FromQuery] string? rol, [FromQuery] string? estado, CancellationToken ct)
    {
        string busqueda = q?.Trim() ?? "";
        if (busqueda.Length > 150) return BadRequest(new { error = "Acortá el texto de búsqueda." });
        return Ok(new { roles = await gestion.RolesDisponibles(ct), usuarios = await gestion.Listar(busqueda, ct, rol, estado), docentes = await asignaciones.Docentes(ct) });
    }

    [HttpPut("{id:int}/estado")]
    public async Task<IActionResult> Estado(int id, EstadoCuenta datos, CancellationToken ct)
    {
        if (!datos.Habilitada.HasValue) return BadRequest(new { error = "Indicá el estado de la cuenta." });
        await gestion.CambiarEstado(int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), id, datos.Habilitada.Value, ct);
        return Ok(new { ok = true });
    }

    [HttpGet("actividad")]
    public async Task<IActionResult> Actividad(CancellationToken ct) => Ok(await complementos.Bitacora(ct));

    [HttpGet("correos")]
    public async Task<IActionResult> Correos(CancellationToken ct) => Ok(await asignaciones.Correos(ct));

    [HttpPut("{id:int}/docente")]
    public async Task<IActionResult> Docente(int id, AsignarDocente datos, CancellationToken ct)
    {
        await asignaciones.Asignar(id, datos.IdDocente, int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), ct);
        return Ok(new { ok = true });
    }

    [HttpPost("{id:int}/recuperacion")]
    public async Task<IActionResult> Recuperacion(int id, CancellationToken ct)
    {
        await recuperacion.Solicitar(await gestion.CorreoCuenta(id, ct), ct);
        return Ok(new { ok = true });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Cambiar(int id, CambioRol datos, CancellationToken ct)
    {
        int actor = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await gestion.Cambiar(actor, id, datos.Rol?.Trim() ?? "", ct);
        return Ok(new { ok = true });
    }

    [HttpPost("correos/{correo:long}/reenviar")]
    public async Task<IActionResult> ReenviarCorreo(long correo, CancellationToken ct)
    {
        int actor = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await registroCorreos.Reenviar(correo, actor, ct);
        return Accepted(new { mensaje = "El aviso quedo pendiente de reenvio" });
    }

    [HttpGet("correos/{correo:long}/intentos")]
    public async Task<IActionResult> IntentosCorreo(long correo, CancellationToken ct)
    {
        return Ok(await registroCorreos.Intentos(correo, ct));
    }

    [HttpGet("correos/recuperaciones")]
    public async Task<IActionResult> CorreosRecuperacion(CancellationToken ct)
    {
        return Ok(await registroCorreos.Recuperaciones(ct));
    }

    public record CambioRol(string? Rol);
    public record AsignarDocente(int? IdDocente);
    public record EstadoCuenta(bool? Habilitada);
}
