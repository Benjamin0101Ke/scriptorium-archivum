using System.Security.Claims;
using APIScriptoriumArchivum.Datos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace APIScriptoriumArchivum.Controllers;

[ApiController]
[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("api/trabajos/{id:int}/valoracion")]
public sealed class ValoracionesController(NpgsqlDataSource db, ConsultaPublica consulta) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Propia(int id, CancellationToken ct)
    {
        await consulta.Buscar(id, ct);
        await using var conexion = await db.OpenConnectionAsync(ct);
        int? estrellas = await Comandos.Valor<short?>(conexion, null,
            "SELECT estrellas FROM public.valoraciones WHERE id_usuario=@usuario AND id_trabajo=@id",
            ct, ("usuario", Actor()), ("id", id));
        return Ok(new { estrellas });
    }

    [HttpPut]
    public async Task<IActionResult> Guardar(int id, DatosValoracion datos, CancellationToken ct)
    {
        if (datos.Estrellas is not (>= 1 and <= 5))
            return BadRequest(new { error = "Elegí una valoración de 1 a 5 estrellas." });
        await consulta.Buscar(id, ct);
        await using var conexion = await db.OpenConnectionAsync(ct);
        await using var tr = await conexion.BeginTransactionAsync(ct);
        if (await Fichas.Bloquear(conexion, tr, id, ct) != "Publicado")
            throw new ErrorApi(409, "El trabajo debe estar publicado para recibir valoraciones.");
        await Comandos.Ejecutar(conexion, tr, """
            INSERT INTO public.valoraciones(id_usuario,id_trabajo,estrellas)
            VALUES(@usuario,@id,@estrellas)
            ON CONFLICT(id_usuario,id_trabajo) DO UPDATE
                SET estrellas=excluded.estrellas,fecha_actualizacion=clock_timestamp()
            """, ct, ("usuario", Actor()), ("id", id), ("estrellas", datos.Estrellas.Value));
        await tr.CommitAsync(ct);
        return Ok(new { estrellas = datos.Estrellas.Value });
    }

    [HttpDelete]
    public async Task<IActionResult> Quitar(int id, CancellationToken ct)
    {
        await using var conexion = await db.OpenConnectionAsync(ct);
        await using var comando = Comandos.Crear(conexion, null,
            "DELETE FROM public.valoraciones WHERE id_usuario=@usuario AND id_trabajo=@id",
            ("usuario", Actor()), ("id", id));
        await comando.ExecuteNonQueryAsync(ct);
        return Ok(new { ok = true });
    }

    private int Actor() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    public record DatosValoracion(int? Estrellas);
}
