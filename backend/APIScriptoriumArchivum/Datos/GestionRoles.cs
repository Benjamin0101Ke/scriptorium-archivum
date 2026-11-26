using APIScriptoriumArchivum.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Text.Json;
using Npgsql;

namespace APIScriptoriumArchivum.Datos;
public sealed class GestionRoles(IDbContextFactory<AppDbContext> baseDeDatos)
{
    private static string[] Permitidos = ["Lector", "Estudiante", "Biblioteca", "Docente revisor", "Administrador", "Superadministrador"];
    public async Task<string[]> RolesDisponibles(CancellationToken ct)
    {
        await using var conexion = await baseDeDatos.CreateDbContextAsync(ct);
        return await Comandos.Valor<string[]>(conexion, null, """
            SELECT array(SELECT nombre FROM public.roles WHERE activo AND nombre = ANY(@roles)
                ORDER BY array_position(@roles, nombre))
            """, ct, ("roles", Permitidos)) ?? [];
    }

    public async Task<List<JsonElement>> Listar(string busqueda, CancellationToken ct, string? rol = null, string? estado = null)
    {
        await using var conexion = await baseDeDatos.CreateDbContextAsync(ct);
        return await Comandos.FilasJson(conexion, """
            SELECT jsonb_build_object('id', u.id_usuario::text, 'nombre', coalesce(u.nombre_mostrar, concat_ws(' ', p.nombres, p.apellidos)),
                'correo', u.correo_institucional, 'rol', r.nombre, 'estado', u.estado, 'id_docente',u.id_docente, 'id_carrera',u.id_carrera, 'id_plan',u.id_plan)::text
            FROM public.usuarios u JOIN public.personas p USING (id_persona) JOIN public.roles r USING (id_rol)
            WHERE (@q = '' OR strpos(lower(concat_ws(' ', u.nombre_mostrar, p.nombres, p.apellidos, u.correo_institucional)), lower(@q)) > 0)
              AND (@rol::text IS NULL OR r.nombre=@rol) AND (@estado::text IS NULL OR u.estado=@estado)
            ORDER BY u.fecha_alta DESC, u.id_usuario DESC LIMIT 100
            """, ct, ("q", busqueda), ("rol", rol), ("estado", estado));
    }

    public async Task CambiarEstado(int actor, int id, bool habilitada, CancellationToken ct)
    {
        if (actor == id)
        {
            throw new ErrorApi(409, "Tu cuenta debe mantenerse habilitada mientras administras el sistema.");
        }

        await using var conexion = await baseDeDatos.CreateDbContextAsync(ct);
        await using var tr = await conexion.Database.BeginTransactionAsync(ct);
        await Autorizar(conexion, tr, actor, id, false, ct);
        string? anterior = await Comandos.Valor<string>(conexion, tr, "SELECT estado FROM public.usuarios WHERE id_usuario=@id FOR UPDATE", ct, ("id", id));
        if (anterior is null)
        {
            throw new ErrorApi(404, "La cuenta no existe.");
        }

        string estado = habilitada ? "Habilitada" : "Deshabilitada";
        if (anterior == estado)
        {
            return;
        }

        await conexion.Usuarios.Where(u => u.IdUsuario == id).ExecuteUpdateAsync(s => s.SetProperty(u => u.Estado, estado).SetProperty(u => u.VersionRol, u => u.VersionRol + 1), ct);
        Registrar(conexion, actor, id, "CAMBIAR_ESTADO_CUENTA", estado);
        await conexion.SaveChangesAsync(ct);
        await tr.CommitAsync(ct);
    }

    public async Task<string> CorreoCuenta(int id, CancellationToken ct)
    {
        await using var conexion = await baseDeDatos.CreateDbContextAsync(ct);
        return await Comandos.Valor<string>(conexion, null, "SELECT correo_institucional FROM public.usuarios WHERE id_usuario=@id AND estado='Habilitada'", ct, ("id", id)) ?? throw new ErrorApi(404, "La cuenta no está habilitada.");
    }

    public async Task Cambiar(int actor, int id, string rol, CancellationToken ct)
    {
        if (!Permitidos.Contains(rol))
        {
            throw new ErrorApi(400, "Elige un rol disponible.");
        }

        if (actor == id)
        {
            throw new ErrorApi(409, "Otra cuenta administradora debe cambiar el rol de tu cuenta.");
        }

        await using var conexion = await baseDeDatos.CreateDbContextAsync(ct);
        await using var tr = await conexion.Database.BeginTransactionAsync(ct);
        await Autorizar(conexion, tr, actor, id, rol == "Superadministrador", ct);
        int proximo = await Comandos.Valor<int>(conexion, tr, "SELECT id_rol FROM public.roles WHERE nombre = @rol AND activo", ct, ("rol", rol));
        if (proximo == 0)
        {
            throw new ErrorApi(400, "Ese rol no está activo.");
        }

        string? previo = await Comandos.Valor<string>(conexion, tr, """
            SELECT r.nombre FROM public.usuarios u JOIN public.roles r USING (id_rol)
            WHERE u.id_usuario = @id AND u.estado = 'Habilitada' FOR UPDATE OF u
            """, ct, ("id", id));
        if (previo is null)
        {
            throw new ErrorApi(404, "La cuenta ya no está habilitada.");
        }

        if (previo == rol)
        {
            return;
        }

        await conexion.Usuarios.Where(u => u.IdUsuario == id).ExecuteUpdateAsync(s => s.SetProperty(u => u.IdRol, proximo).SetProperty(u => u.VersionRol, u => u.VersionRol + 1), ct);
        Registrar(conexion, actor, id, "CAMBIAR_ROL", $"{previo} → {rol}");
        await conexion.SaveChangesAsync(ct);
        await tr.CommitAsync(ct);
    }

    private static async Task Autorizar(AppDbContext c, IDbContextTransaction tr, int actor, int id, bool asignaSuper, CancellationToken ct)
    {
        // Serializa cambios de privilegios entre administradores.
        await Comandos.FilasJson(c, tr, "SELECT to_json(id_rol)::text FROM public.roles WHERE nombre IN ('Administrador','Superadministrador') ORDER BY id_rol FOR UPDATE", ct);
        string? rolActor = await Comandos.Valor<string>(c, tr, "SELECT r.nombre FROM public.usuarios u JOIN public.roles r USING(id_rol) WHERE u.id_usuario=@id AND u.estado='Habilitada' AND r.activo FOR UPDATE OF u", ct, ("id", actor));
        if (rolActor is not ("Administrador" or "Superadministrador"))
        {
            throw new ErrorApi(403, "Se requiere una cuenta administradora.");
        }

        string? rolDestino = await Comandos.Valor<string>(c, tr, "SELECT r.nombre FROM public.usuarios u JOIN public.roles r USING(id_rol) WHERE u.id_usuario=@id FOR UPDATE OF u", ct, ("id", id));
        if (rolDestino is null)
        {
            throw new ErrorApi(404, "La cuenta no existe.");
        }

        if ((asignaSuper || rolDestino == "Superadministrador") && rolActor != "Superadministrador")
        {
            throw new ErrorApi(403, "Solo un superadministrador puede gestionar cuentas de superadministrador.");
        }
    }

    public async Task Corregir(int actor, int id, string? nombre, int? carrera, int? plan, CancellationToken ct)
    {
        nombre = nombre?.Trim();
        if (string.IsNullOrWhiteSpace(nombre) || nombre.Length > 120)
        {
            throw new ErrorApi(400, "Escribe un nombre de entre 1 y 120 caracteres.");
        }

        await using var c = await baseDeDatos.CreateDbContextAsync(ct);
        await using var tr = await c.Database.BeginTransactionAsync(ct);
        await Autorizar(c, tr, actor, id, false, ct);
        await Usuarios.ValidarCarrera(c, tr, carrera, ct);
        await Usuarios.ValidarPlan(c, tr, carrera, plan, ct);
        string? antes = await Comandos.Valor<string>(c, tr, "SELECT jsonb_build_object('nombre',nombre_mostrar,'id_carrera',id_carrera,'id_plan',id_plan)::text FROM public.usuarios WHERE id_usuario=@id", ct, ("id", id));
        string despues = JsonSerializer.Serialize(new { nombre, id_carrera = carrera, id_plan = plan });
        await c.Usuarios.Where(u => u.IdUsuario == id).ExecuteUpdateAsync(s => s.SetProperty(u => u.NombreMostrar, nombre).SetProperty(u => u.IdCarrera, carrera).SetProperty(u => u.IdPlan, plan), ct);
        Registrar(c, actor, id, "CORREGIR_PERFIL", $"Antes: {antes}. Después: {despues}");
        await c.SaveChangesAsync(ct);
        await tr.CommitAsync(ct);
    }

    private static void Registrar(AppDbContext contexto, int actor, int usuario, string operacion, string detalle)
    {
        contexto.Bitacora.Add(new APIScriptoriumArchivum.Models.Bitacora { IdUsuarioActor = actor, Operacion = operacion, TablaAfectada = "usuarios", RegistroAfectado = usuario.ToString(), Detalle = detalle });
    }
}
