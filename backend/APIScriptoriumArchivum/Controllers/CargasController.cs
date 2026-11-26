using System.Security.Claims;
using APIScriptoriumArchivum.Datos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APIScriptoriumArchivum.Controllers;
// Recibe archivos por partes y controla que la carga pertenezca al usuario conectado
[ApiController]
[Authorize(Roles = "Biblioteca,Lector,Estudiante")]
[Route("api/cargas")]
public sealed class CargasController(CargasArchivos cargas) : ControllerBase
{
    // Identifica al propietario de la carga desde su sesion
    private int Actor()
    {
        return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }

    // Inicia una carga temporal y devuelve el tamano permitido de cada parte
    [HttpPost]
    public async Task<IActionResult> Crear(NuevaCarga datos, CancellationToken ct)
    {
        Guid id = await cargas.Crear(Actor(), datos.Nombre, datos.TamanoBytes, ct);
        return StatusCode(201, new { id, parte_bytes = CargasArchivos.ParteBytes });
    }

    // Agrega una parte del archivo a la carga del usuario
    [HttpPut("{id:guid}/partes/{parte:long}")]
    [RequestSizeLimit(CargasArchivos.ParteBytes)]
    public async Task<IActionResult> Parte(Guid id, long parte, CancellationToken ct)
    {
        long recibidos = await cargas.Agregar(id, Actor(), parte, Request.Body, Request.ContentLength, ct);
        return Ok(new { recibidos });
    }

    // Cancela la carga y retira los archivos temporales asociados
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Borrar(Guid id, CancellationToken ct)
    {
        await cargas.Borrar(id, Actor(), ct);
        return Ok(new { ok = true });
    }

    // Nombre y tamano declarados antes de comenzar una carga por partes
    public sealed record NuevaCarga(string? Nombre, long TamanoBytes);
}
