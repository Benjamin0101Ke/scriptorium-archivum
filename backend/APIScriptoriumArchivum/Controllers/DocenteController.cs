using System.Security.Claims;
using System.Text.Json;
using APIScriptoriumArchivum.Datos;
using APIScriptoriumArchivum.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace APIScriptoriumArchivum.Controllers;

[ApiController]
[Authorize(Roles = "Docente revisor")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("api/docente")]
public class DocenteController(RevisionesDocentes revisiones) : ControllerBase
{
    private bool Super=>User.IsInRole("Superadministrador");
    private int Actor() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> Panel(CancellationToken ct)=>Ok(await revisiones.Panel(Actor(),Super,ct));

    [HttpGet("trabajos/{id:int}/revisiones")]
    public async Task<IActionResult> Historial(int id,CancellationToken ct){await revisiones.Autorizar(id,Actor(),Super,ct);return Ok(await revisiones.Historial(id,ct));}
    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen(CancellationToken ct)
    {
        return Ok(await revisiones.Resumen(Actor(), ct, Super));
    }

    [HttpGet("trabajos")]
    public async Task<IActionResult> Trabajos([FromQuery] string? q, [FromQuery] string? estado, [FromQuery] int? estudiante, CancellationToken ct)
    {
        string? filtro = estado?.Trim() ?? "todas";
        if (filtro is not ("todas" or "pendiente" or "conforme" or "cambios"))
        {
            return BadRequest(new { error = "El estado de revisión solicitado no es válido." });
        }
        return Ok(await revisiones.Listar(Actor(), q, filtro, estudiante, ct, Super));
    }

    [HttpGet("trabajos/{id:int}")]
    public async Task<IActionResult> Detalle(int id, CancellationToken ct)
    {
        return Ok(await revisiones.Detalle(Actor(), id, ct, Super));
    }

    [HttpGet("trabajos/{id:int}/pdf")]
    public async Task<IActionResult> Pdf(int id, [FromQuery] int? ejemplar, CancellationToken ct)
    {
        string ruta = await revisiones.Pdf(Actor(), id, ct, Super, ejemplar);
        Response.Headers.CacheControl = "no-store";
        Response.Headers["Content-Disposition"] = $"inline; filename=\"trabajo-{id}.pdf\"";
        return PhysicalFile(ruta, "application/pdf", enableRangeProcessing: true);
    }

    [HttpPost("trabajos/{id:int}/revisiones")]
    public async Task<IActionResult> Revisar(int id, RevisionDocenteDatos datos, CancellationToken ct)
    {
        JsonElement revision = await revisiones.Registrar(Actor(), id, datos, ct, Super);
        return StatusCode(201,new { revision });
    }
}

[ApiController,Authorize,ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
public sealed class HistorialDocenteController(RevisionesDocentes revisiones,NpgsqlDataSource db):ControllerBase
{
    [Authorize(Roles="Biblioteca"),HttpGet("api/admin/trabajos/{id:int}/revisiones")]
    public async Task<IActionResult> Biblioteca(int id,CancellationToken ct)=>Ok(await revisiones.Historial(id,ct));
    [Authorize(Roles="Lector,Estudiante"),HttpGet("api/mis-envios/{id:int}/revisiones")]
    public async Task<IActionResult> Alumno(int id,CancellationToken ct)
    {
        await using var c=await db.OpenConnectionAsync(ct);
        if(!await Comandos.Valor<bool>(c,null,"SELECT EXISTS(SELECT 1 FROM public.trabajos WHERE id_trabajo=@id AND id_usuario_envio=@actor)",ct,
            ("id",id),("actor",int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!))))return NotFound(new{error="El envío no está disponible."});
        return Ok(await revisiones.Historial(id,ct));
    }
}
