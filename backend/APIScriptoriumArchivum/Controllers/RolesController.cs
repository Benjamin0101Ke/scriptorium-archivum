using System.Security.Claims;
using APIScriptoriumArchivum.Datos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APIScriptoriumArchivum.Controllers;
// Centraliza la administracion de cuentas, asignaciones docentes y registros de correo
[ApiController]
[Authorize(Roles = "Administrador")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("api/admin/[controller]")]
public sealed class RolesController(GestionRoles gestion, Complementos complementos, Recuperacion recuperacion, AsignacionesDocentes asignaciones, RegistroCorreos registroCorreos) : ControllerBase
{
    // Busca cuentas y devuelve los roles y docentes disponibles
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] string? q, [FromQuery] string? rol, [FromQuery] string? estado, CancellationToken ct)
    {
        string busqueda = q?.Trim() ?? "";
        if (busqueda.Length > 150)
        {
            return BadRequest(new { error = "Acorta el texto de búsqueda." });
        }

        return Ok(new { roles = await gestion.RolesDisponibles(ct), usuarios = await gestion.Listar(busqueda, ct, rol, estado), docentes = await asignaciones.Docentes(ct) });
    }

    // Habilita o deshabilita la cuenta seleccionada
    [HttpPut("{id:int}/estado")]
    public async Task<IActionResult> Estado(int id, EstadoCuenta datos, CancellationToken ct)
    {
        if (!datos.Habilitada.HasValue)
        {
            return BadRequest(new { error = "Indica el estado de la cuenta." });
        }

        await gestion.CambiarEstado(int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), id, datos.Habilitada.Value, ct);
        return Ok(new { ok = true });
    }

    // Aplica una correccion administrativa del nombre, carrera o plan
    [HttpPut("{id:int}/perfil")]
    public async Task<IActionResult> Perfil(int id, CorreccionPerfil datos, CancellationToken ct)
    {
        await gestion.Corregir(int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), id, datos.Nombre, datos.IdCarrera, datos.IdPlan, ct);
        return Ok(new { ok = true });
    }

    // Consulta la bitacora disponible para Administracion
    [HttpGet("actividad")]
    public async Task<IActionResult> Actividad(CancellationToken ct)
    {
        return Ok(await complementos.Bitacora(ct));
    }

    // Lista los avisos de correo y su estado de envio
    [HttpGet("correos")]
    public async Task<IActionResult> Correos(CancellationToken ct)
    {
        return Ok(await asignaciones.Correos(ct));
    }

    // Asigna o retira el docente del estudiante mediante Administracion
    [HttpPut("{id:int}/docente")]
    public async Task<IActionResult> Docente(int id, AsignarDocente datos, CancellationToken ct)
    {
        await asignaciones.Asignar(id, datos.IdDocente, int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), ct);
        return Ok(new { ok = true });
    }

    // Solicita la recuperacion de acceso de la cuenta seleccionada
    [HttpPost("{id:int}/recuperacion")]
    public async Task<IActionResult> Recuperacion(int id, CancellationToken ct)
    {
        await recuperacion.Solicitar(await gestion.CorreoCuenta(id, ct), ct);
        return Ok(new { ok = true });
    }

    // Asigna el rol permitido y registra el cambio de la cuenta
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Cambiar(int id, CambioRol datos, CancellationToken ct)
    {
        int actor = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await gestion.Cambiar(actor, id, datos.Rol?.Trim() ?? "", ct);
        return Ok(new { ok = true });
    }

    // Inicia otro ciclo de envio para un aviso que termino fallido
    [HttpPost("correos/{correo:long}/reenviar")]
    public async Task<IActionResult> ReenviarCorreo(long correo, CancellationToken ct)
    {
        int actor = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await registroCorreos.Reenviar(correo, actor, ct);
        return Accepted(new { mensaje = "El aviso quedo pendiente de reenvio" });
    }

    // Consulta los intentos del aviso conservando su historial de ciclos
    [HttpGet("correos/{correo:long}/intentos")]
    public async Task<IActionResult> IntentosCorreo(long correo, CancellationToken ct)
    {
        return Ok(await registroCorreos.Intentos(correo, ct));
    }

    // Muestra los resultados tecnicos de envio de codigos de recuperacion
    [HttpGet("correos/recuperaciones")]
    public async Task<IActionResult> CorreosRecuperacion(CancellationToken ct)
    {
        return Ok(await registroCorreos.Recuperaciones(ct));
    }

    // Nombre del rol que se solicita asignar a la cuenta
    public record CambioRol(string? Rol);
    // Identificador del docente que Administracion asigna al estudiante
    public record AsignarDocente(int? IdDocente);
    // Indica si la cuenta debe quedar habilitada o deshabilitada
    public record EstadoCuenta(bool? Habilitada);
    // Correcciones administrativas del nombre y los datos academicos de una cuenta
    public record CorreccionPerfil(string? Nombre, int? IdCarrera, int? IdPlan);
}
