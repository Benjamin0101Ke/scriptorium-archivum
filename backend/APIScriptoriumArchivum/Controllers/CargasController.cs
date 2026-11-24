using System.Security.Claims;
using APIScriptoriumArchivum.Datos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APIScriptoriumArchivum.Controllers;

[ApiController]
[Authorize(Roles = "Biblioteca,Lector,Estudiante")]
[Route("api/cargas")]
public sealed class CargasController(CargasArchivos cargas) : ControllerBase
{
    private int Actor() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost]
    public async Task<IActionResult> Crear(NuevaCarga datos, CancellationToken ct)
    {
        Guid id = await cargas.Crear(Actor(), datos.Nombre, datos.TamanoBytes, ct);
        return StatusCode(201, new { id, parte_bytes = CargasArchivos.ParteBytes });
    }

    [HttpPut("{id:guid}/partes/{parte:long}")]
    [RequestSizeLimit(CargasArchivos.ParteBytes)]
    public async Task<IActionResult> Parte(Guid id, long parte, CancellationToken ct)
    {
        long recibidos = await cargas.Agregar(id, Actor(), parte, Request.Body, Request.ContentLength, ct);
        return Ok(new { recibidos });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Borrar(Guid id, CancellationToken ct)
    {
        await cargas.Borrar(id, Actor(), ct);
        return Ok(new { ok = true });
    }

    public sealed record NuevaCarga(string? Nombre, long TamanoBytes);
}
