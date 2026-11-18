using System.Text.Json;
using Npgsql;

namespace APIScriptoriumArchivum.Datos;

public sealed class GestionRoles(NpgsqlDataSource baseDeDatos)
{
    private static readonly string[] Permitidos = ["Lector", "Estudiante", "Biblioteca", "Docente revisor", "Administrador"];

    public async Task<string[]> RolesDisponibles(CancellationToken ct)
    {
        await using var conexion = await baseDeDatos.OpenConnectionAsync(ct);
        return await Comandos.Valor<string[]>(conexion, null, """
            SELECT array(SELECT nombre FROM public.roles WHERE activo AND nombre = ANY(@roles)
                ORDER BY array_position(@roles, nombre))
            """, ct, ("roles", Permitidos)) ?? [];
    }

    public async Task<List<JsonElement>> Listar(string busqueda, CancellationToken ct, string? rol = null, string? estado = null)
    {
        await using var conexion = await baseDeDatos.OpenConnectionAsync(ct);
        return await Comandos.FilasJson(conexion, """
            SELECT jsonb_build_object('id', u.id_usuario::text, 'nombre', coalesce(u.nombre_mostrar, concat_ws(' ', p.nombres, p.apellidos)),
                'correo', u.correo_institucional, 'rol', r.nombre, 'estado', u.estado, 'id_docente',u.id_docente)::text
            FROM public.usuarios u JOIN public.personas p USING (id_persona) JOIN public.roles r USING (id_rol)
            WHERE (@q = '' OR strpos(lower(concat_ws(' ', u.nombre_mostrar, p.nombres, p.apellidos, u.correo_institucional)), lower(@q)) > 0)
              AND (@rol::text IS NULL OR r.nombre=@rol) AND (@estado::text IS NULL OR u.estado=@estado)
            ORDER BY u.fecha_alta DESC, u.id_usuario DESC LIMIT 100
            """, ct, ("q", busqueda), ("rol", rol), ("estado", estado));
    }

    public async Task CambiarEstado(int actor, int id, bool habilitada, CancellationToken ct)
    {
        if (actor == id) throw new ErrorApi(409, "Tu cuenta debe mantenerse habilitada mientras administrás el sistema.");
        await using var conexion = await baseDeDatos.OpenConnectionAsync(ct);
        await using var tr = await conexion.BeginTransactionAsync(ct);
        int admin = await Comandos.Valor<int>(conexion, tr,
            "SELECT id_rol FROM public.roles WHERE nombre='Administrador' AND activo FOR UPDATE", ct);
        int actorRol = await Comandos.Valor<int>(conexion, tr,
            "SELECT id_rol FROM public.usuarios WHERE id_usuario=@actor AND estado='Habilitada'", ct, ("actor", actor));
        if (actorRol != admin || admin == 0) throw new ErrorApi(403, "Se requiere una cuenta administradora.");
        string? anterior = await Comandos.Valor<string>(conexion, tr,
            "SELECT estado FROM public.usuarios WHERE id_usuario=@id FOR UPDATE", ct, ("id", id));
        if (anterior is null) throw new ErrorApi(404, "La cuenta no existe.");
        string estado = habilitada ? "Habilitada" : "Deshabilitada";
        if (anterior == estado) return;
        await Comandos.Ejecutar(conexion, tr, """
            UPDATE public.usuarios SET estado=@estado, version_rol=version_rol+1 WHERE id_usuario=@id;
            INSERT INTO public.bitacora(id_usuario_actor,operacion,tabla_afectada,registro_afectado,detalle)
            VALUES(@actor,'CAMBIAR_ESTADO_CUENTA','usuarios',@id::text,@estado);
            """, ct, ("id", id), ("actor", actor), ("estado", estado));
        await tr.CommitAsync(ct);
    }

    public async Task<string> CorreoCuenta(int id, CancellationToken ct)
    {
        await using var conexion = await baseDeDatos.OpenConnectionAsync(ct);
        return await Comandos.Valor<string>(conexion, null,
            "SELECT correo_institucional FROM public.usuarios WHERE id_usuario=@id AND estado='Habilitada'", ct,
            ("id", id)) ?? throw new ErrorApi(404, "La cuenta no está habilitada.");
    }

    public async Task Cambiar(int actor, int id, string rol, CancellationToken ct)
    {
        if (!Permitidos.Contains(rol)) throw new ErrorApi(400, "Elegí un rol disponible.");
        if (actor == id) throw new ErrorApi(409, "Otra cuenta administradora debe cambiar el rol de tu cuenta.");

        await using var conexion = await baseDeDatos.OpenConnectionAsync(ct);
        await using var tr = await conexion.BeginTransactionAsync(ct);
        // Las asignaciones se serializan para validar al administrador antes de aplicar otro cambio.
        int admin = await Comandos.Valor<int>(conexion, tr,
            "SELECT id_rol FROM public.roles WHERE nombre = 'Administrador' AND activo FOR UPDATE", ct);
        int actorRol = await Comandos.Valor<int>(conexion, tr, """
            SELECT id_rol FROM public.usuarios WHERE id_usuario = @actor AND estado = 'Habilitada'
            """, ct, ("actor", actor));
        if (admin == 0 || actorRol != admin) throw new ErrorApi(403, "Se requiere una cuenta administradora.");

        int proximo = await Comandos.Valor<int>(conexion, tr,
            "SELECT id_rol FROM public.roles WHERE nombre = @rol AND activo", ct, ("rol", rol));
        if (proximo == 0) throw new ErrorApi(400, "Ese rol no está activo.");
        string? previo = await Comandos.Valor<string>(conexion, tr, """
            SELECT r.nombre FROM public.usuarios u JOIN public.roles r USING (id_rol)
            WHERE u.id_usuario = @id AND u.estado = 'Habilitada' FOR UPDATE OF u
            """, ct, ("id", id));
        if (previo is null) throw new ErrorApi(404, "La cuenta ya no está habilitada.");
        if (previo == rol) return;

        await Comandos.Ejecutar(conexion, tr, """
            UPDATE public.usuarios SET id_rol = @proximo, version_rol = version_rol + 1 WHERE id_usuario = @id;
            INSERT INTO public.bitacora (id_usuario_actor, operacion, tabla_afectada, registro_afectado, detalle)
            VALUES (@actor, 'CAMBIAR_ROL', 'usuarios', @id::text, @detalle)
            """, ct, ("id", id), ("proximo", proximo), ("actor", actor), ("detalle", $"{previo} → {rol}"));
        await tr.CommitAsync(ct);
    }
}
