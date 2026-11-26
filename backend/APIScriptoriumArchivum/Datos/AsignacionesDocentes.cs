using APIScriptoriumArchivum.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Text.Json;
using Npgsql;

namespace APIScriptoriumArchivum.Datos;
public sealed class AsignacionesDocentes(IDbContextFactory<AppDbContext> db, Avisos avisos)
{
    public async Task<List<JsonElement>> Docentes(CancellationToken ct)
    {
        await using var c = await db.CreateDbContextAsync(ct);
        return await Comandos.FilasJson(c, """
            SELECT jsonb_build_object('id',u.id_usuario,'nombre',coalesce(u.nombre_mostrar,concat_ws(' ',p.nombres,p.apellidos)),
                'correo',u.correo_institucional)::text FROM public.usuarios u
            JOIN public.personas p USING(id_persona) JOIN public.roles r USING(id_rol)
            WHERE r.nombre IN ('Docente revisor','Superadministrador') AND r.activo AND u.estado='Habilitada'
            ORDER BY p.apellidos,p.nombres,u.id_usuario
            """, ct);
    }

    public async Task Asignar(int alumno, int? docente, int actor, CancellationToken ct)
    {
        await using var c = await db.CreateDbContextAsync(ct);
        await using var tr = await c.Database.BeginTransactionAsync(ct);
        var alumnos = await c.Usuarios.FromSqlRaw("""
            SELECT u.* FROM public.usuarios u JOIN public.roles r USING(id_rol)
            WHERE u.id_usuario=@id AND u.estado='Habilitada' AND r.nombre='Estudiante' AND r.activo
            FOR UPDATE OF u
            """, Comandos.Parametros(("id", alumno))).ToListAsync(ct);
        var estudiante = alumnos.SingleOrDefault();
        if (estudiante is null)
        {
            throw new ErrorApi(409, "Selecciona una cuenta estudiantil habilitada.");
        }

        if (estudiante.IdDocente == docente)
        {
            return;
        }

        if (docente.HasValue && !await c.Usuarios.AnyAsync(u => u.IdUsuario == docente.Value && u.Estado == "Habilitada" && u.IdRolNavigation.Activo && (u.IdRolNavigation.Nombre == "Docente revisor" || u.IdRolNavigation.Nombre == "Superadministrador"), ct))
        {
            throw new ErrorApi(400, "Selecciona un docente revisor habilitado.");
        }

        estudiante.IdDocente = docente;
        await c.SaveChangesAsync(ct);
        await avisos.Asignacion(c, tr, alumno, docente, actor, ct);
        await tr.CommitAsync(ct);
    }

    public async Task<List<JsonElement>> Correos(CancellationToken ct)
    {
        await using var c = await db.CreateDbContextAsync(ct);
        return await Comandos.FilasJson(c, """
            SELECT jsonb_build_object('id',m.id_correo::text,'destino',m.destino,'asunto',m.asunto,'estado',m.estado,
                'intentos',m.intentos,'ciclo',m.ciclo,'proximo_intento',m.proximo_intento,'fecha',m.creado_en,'enviado_en',m.enviado_en,'error_tipo',m.error_tipo,
                'evento',b.operacion)::text
            FROM public.correos_eventos m JOIN public.bitacora b USING(id_evento)
            ORDER BY m.id_correo DESC LIMIT 100
            """, ct);
    }
}
