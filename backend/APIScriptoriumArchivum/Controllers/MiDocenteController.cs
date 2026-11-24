using System.Security.Claims;
using APIScriptoriumArchivum.Datos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace APIScriptoriumArchivum.Controllers;

[ApiController, Authorize(Roles = "Estudiante"), Route("api/mi-docente")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class MiDocenteController(NpgsqlDataSource db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Consultar(CancellationToken ct)
    {
        int actor = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await using var c = await db.OpenConnectionAsync(ct);
        var rows = await Comandos.FilasJson(c, """
            SELECT jsonb_build_object(
              'estado',CASE WHEN a.id_docente IS NULL THEN 'Sin asignación'
                WHEN d.estado='Habilitada' AND r.activo AND r.nombre IN ('Docente revisor','Superadministrador')
                  THEN 'Asignado' ELSE 'No disponible' END,
              'docente',CASE WHEN d.estado='Habilitada' AND r.activo AND r.nombre IN ('Docente revisor','Superadministrador')
                THEN jsonb_build_object('nombre',coalesce(d.nombre_mostrar,concat_ws(' ',p.nombres,p.apellidos)),
                  'correo',d.correo_institucional) ELSE NULL END)::text
            FROM public.usuarios a LEFT JOIN public.usuarios d ON d.id_usuario=a.id_docente
            LEFT JOIN public.roles r ON r.id_rol=d.id_rol LEFT JOIN public.personas p ON p.id_persona=d.id_persona
            WHERE a.id_usuario=@actor AND a.estado='Habilitada'
            """, ct, ("actor",actor));
        return rows.Count == 0 ? NotFound(new { error = "La cuenta no está disponible." }) : Ok(rows[0]);
    }
}
