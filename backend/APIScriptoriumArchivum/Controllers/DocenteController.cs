using System.Security.Claims;
using System.Text.Json;
using APIScriptoriumArchivum.Datos;
using APIScriptoriumArchivum.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APIScriptoriumArchivum.Controllers;

[ApiController]
[Authorize(Roles = "Docente revisor")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("api/docente")]
public class DocenteController(RevisionesDocentes revisiones) : ControllerBase
{
    private int Actor() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen(CancellationToken ct)
    {
        return Ok(await revisiones.Resumen(Actor(), ct));
    }

    [HttpGet("trabajos")]
    public async Task<IActionResult> Trabajos([FromQuery] string? q, [FromQuery] string? estado, [FromQuery] int? estudiante, CancellationToken ct)
    {
        string? filtro = estado?.Trim() ?? "todas";
        if (filtro is not ("todas" or "pendiente" or "conforme" or "cambios"))
        {
            return BadRequest(new { error = "El estado de revisión solicitado no es válido." });
        }
        return Ok(await revisiones.Listar(Actor(), q, filtro, estudiante, ct));
    }

    [HttpGet("trabajos/{id:int}")]
    public async Task<IActionResult> Detalle(int id, CancellationToken ct)
    {
        return Ok(await revisiones.Detalle(Actor(), id, ct));
    }

    [HttpGet("trabajos/{id:int}/pdf")]
    public async Task<IActionResult> Pdf(int id, CancellationToken ct)
    {
        string ruta = await revisiones.Pdf(Actor(), id, ct);
        Response.Headers.CacheControl = "no-store";
        Response.Headers["Content-Disposition"] = $"inline; filename=\"trabajo-{id}.pdf\"";
        return PhysicalFile(ruta, "application/pdf", enableRangeProcessing: true);
    }

    [HttpPost("trabajos/{id:int}/revisiones")]
    public async Task<IActionResult> Revisar(int id, RevisionDocenteDatos datos, CancellationToken ct)
    {
        JsonElement revision = await revisiones.Registrar(Actor(), id, datos, ct);
        return Ok(new { revision });
    }
}
