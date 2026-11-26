using APIScriptoriumArchivum.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Text.Json;
using Npgsql;

namespace APIScriptoriumArchivum.Datos;
public sealed class Presencia(IDbContextFactory<AppDbContext> db)
{
    public async Task Marcar(Cuenta cuenta, Guid sesion, CancellationToken ct)
    {
        await using var c = await db.CreateDbContextAsync(ct);
        await using var tr = await c.Database.BeginTransactionAsync(ct);
        await Comandos.Ejecutar(c, tr, """
            DELETE FROM public.presencia_sesiones WHERE ultima_actividad < now()-interval '1 day';
            INSERT INTO public.presencia_sesiones(id_sesion,id_usuario,version_sesion,ultima_actividad)
            VALUES(@sesion,@id,@version,now()) ON CONFLICT(id_sesion) DO UPDATE
            SET ultima_actividad=now(),version_sesion=excluded.version_sesion
            WHERE presencia_sesiones.id_usuario=excluded.id_usuario;
            """, ct, ("sesion", sesion), ("id", cuenta.Id), ("version", cuenta.VersionSesion));
        await tr.CommitAsync(ct);
    }

    public async Task Salir(int id, CancellationToken ct)
    {
        await using var c = await db.CreateDbContextAsync(ct);
        await c.PresenciaSesiones.Where(s => s.IdUsuario == id).ExecuteDeleteAsync(ct);
    }

    public async Task<List<JsonElement>> Conectados(CancellationToken ct)
    {
        await using var c = await db.CreateDbContextAsync(ct);
        return await Comandos.FilasJson(c, """
            SELECT jsonb_build_object('id',u.id_usuario::text,'nombre',coalesce(u.nombre_mostrar,concat_ws(' ',p.nombres,p.apellidos)),
                'rol',r.nombre,'ultima_actividad',max(s.ultima_actividad))::text
            FROM public.presencia_sesiones s JOIN public.usuarios u USING(id_usuario)
            JOIN public.personas p USING(id_persona) JOIN public.roles r USING(id_rol)
            WHERE s.ultima_actividad > now()-interval '2 minutes' AND u.estado='Habilitada' AND r.activo
                AND s.version_sesion=upper(encode(sha256(convert_to(CASE WHEN u.version_rol=0 THEN u.hash_contrasena
                    ELSE u.hash_contrasena||':'||u.version_rol::text END,'UTF8')),'hex'))
            GROUP BY u.id_usuario,p.nombres,p.apellidos,r.nombre ORDER BY max(s.ultima_actividad) DESC
            """, ct);
    }
}
