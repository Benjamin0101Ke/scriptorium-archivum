using APIScriptoriumArchivum.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace APIScriptoriumArchivum.Datos;
public sealed class Avisos(IDbContextFactory<AppDbContext> db, IConfiguration config)
{
    public static string FechaLocal()
    {
        return TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTimeOffset.UtcNow, "America/El_Salvador").ToString("dd/MM/yyyy HH:mm '— El Salvador'");
    }

    private static async Task<int> Evento(AppDbContext c, IDbContextTransaction tr, int actor, string tipo, string tabla, int id, string detalle, CancellationToken ct)
    {
        var evento = new APIScriptoriumArchivum.Models.Bitacora
        {
            IdUsuarioActor = actor,
            Operacion = tipo,
            TablaAfectada = tabla,
            RegistroAfectado = id.ToString(),
            Detalle = detalle
        };
        c.Bitacora.Add(evento);
        await c.SaveChangesAsync(ct);
        return evento.IdEvento;
    }

    private static async Task Encolar(AppDbContext c, IDbContextTransaction tr, int evento, string destino, string titulo, string texto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(destino))
        {
            return;
        }

        await Comandos.Ejecutar(c, tr, """
            INSERT INTO public.correos_eventos(id_evento,destino,asunto,titulo,mensaje)
            VALUES(@evento,lower(@destino),@asunto,@titulo,@texto)
            ON CONFLICT(id_evento,destino) DO NOTHING
            """, ct, ("evento", evento), ("destino", destino), ("asunto", titulo + " · Scriptorium Archivum"), ("titulo", titulo), ("texto", texto));
    }

    public async Task Cuenta(AppDbContext c, IDbContextTransaction tr, int id, string tipo, CancellationToken ct)
    {
        var cuentas = await Comandos.FilasJson(c, tr, """
            SELECT jsonb_build_object('nombre',coalesce(u.nombre_mostrar,concat_ws(' ',p.nombres,p.apellidos)),
              'correo',u.correo_institucional,'rol',r.nombre)::text
            FROM public.usuarios u JOIN public.personas p USING(id_persona) JOIN public.roles r USING(id_rol)
            WHERE u.id_usuario=@id
            """, ct, ("id", id));
        var cuenta = cuentas[0];
        string nombre = cuenta.GetProperty("nombre").GetString()!;
        string correo = cuenta.GetProperty("correo").GetString()!;
        string rol = cuenta.GetProperty("rol").GetString()!;
        bool registro = tipo == "REGISTRO_CUENTA";
        string titulo = registro ? "Nueva cuenta registrada" : tipo == "CIERRE_SESION" ? "Sesión cerrada" : "Inicio de sesión";
        string texto = $"{nombre}\nCorreo: {correo}\nRol: {rol}\nAcción: {titulo}\nFecha: {FechaLocal()}";
        int evento = await Evento(c, tr, id, tipo, "usuarios", id, texto, ct);
        if (registro || tipo == "INICIO_SESION" && config.GetValue("Correo:AvisosAcceso", true))
        {
            await Encolar(c, tr, evento, string.IsNullOrWhiteSpace(config["Correo:Avisos"]) ? config["Correo:Remitente"] ?? "no-responder@scriptorium.test" : config["Correo:Avisos"]!, titulo, texto, ct);
        }

        if (registro)
        {
            await Encolar(c, tr, evento, correo, "Tu cuenta está registrada", $"Hola, {nombre}.\nTu cuenta de {rol} se registró correctamente. Ya puedes ingresar a Scriptorium Archivum.\nFecha: {FechaLocal()}", ct);
        }
    }

    public async Task Acceso(int id, string tipo, CancellationToken ct)
    {
        await using var c = await db.CreateDbContextAsync(ct);
        await using var tr = await c.Database.BeginTransactionAsync(ct);
        await Cuenta(c, tr, id, tipo, ct);
        await tr.CommitAsync(ct);
    }

    public async Task Trabajo(AppDbContext c, IDbContextTransaction tr, int id, int actor, string tipo, string titulo, string cambio, bool avisarBiblioteca, CancellationToken ct)
    {
        var fichas = await Comandos.FilasJson(c, tr, """
            SELECT jsonb_build_object('titulo',t.titulo,'tipo',tt.nombre,'estado',t.estado_publicacion,
                'alumno',coalesce(u.nombre_mostrar,concat_ws(' ',p.nombres,p.apellidos)))::text
            FROM public.trabajos t JOIN public.tipos_trabajo tt USING(id_tipo_trabajo)
            LEFT JOIN public.usuarios u ON u.id_usuario=t.id_usuario_envio
            LEFT JOIN public.personas p ON p.id_persona=u.id_persona WHERE t.id_trabajo=@id
            """, ct, ("id", id));
        var ficha = fichas[0];
        string texto = $"Trabajo: {ficha.GetProperty("titulo").GetString()}\nTipo: {ficha.GetProperty("tipo").GetString()}\nAutor de la cuenta: {ficha.GetProperty("alumno").GetString()}\nEstado: {ficha.GetProperty("estado").GetString()}\n\n{cambio}\n\nFecha: {FechaLocal()}";
        int evento = await Evento(c, tr, actor, tipo, "trabajos", id, texto, ct);
        var destinos = await Comandos.FilasJson(c, tr, """
            SELECT DISTINCT jsonb_build_object('id',u.id_usuario,'correo',u.correo_institucional)::text
            FROM public.usuarios u JOIN public.roles r USING(id_rol)
            WHERE u.estado='Habilitada' AND r.activo AND (
              u.id_usuario IN (SELECT id_usuario_envio FROM public.trabajos WHERE id_trabajo=@id)
              OR (r.nombre IN ('Lector','Estudiante') AND u.id_persona IN (
                SELECT id_persona FROM public.participaciones WHERE id_trabajo=@id AND calidad_participacion='Autor'))
              OR (r.nombre IN ('Docente revisor','Superadministrador') AND u.id_usuario IN (
                SELECT a.id_docente FROM public.usuarios a WHERE a.id_usuario IN (
                    SELECT id_usuario_envio FROM public.trabajos WHERE id_trabajo=@id)
                  OR a.id_persona IN (SELECT id_persona FROM public.participaciones
                    WHERE id_trabajo=@id AND calidad_participacion='Autor')))
              OR (@biblioteca AND r.nombre='Biblioteca'))
            """, ct, ("id", id), ("biblioteca", avisarBiblioteca));
        foreach (var destinatario in destinos)
        {
            int usuario = destinatario.GetProperty("id").GetInt32();
            c.Notificaciones.Add(new APIScriptoriumArchivum.Models.Notificaciones { IdTrabajo = id, TipoEvento = tipo, Mensaje = texto, IdUsuario = usuario });
            await c.SaveChangesAsync(ct);
            await Encolar(c, tr, evento, destinatario.GetProperty("correo").GetString()!, titulo, texto, ct);
        }
    }

    public async Task Asignacion(AppDbContext c, IDbContextTransaction tr, int alumno, int? docente, int actor, CancellationToken ct)
    {
        int evento = await Evento(c, tr, actor, "ASIGNAR_DOCENTE", "usuarios", alumno, docente.HasValue ? $"Docente asignado: {docente.Value}" : "Asignación de docente retirada", ct);
        var cuentas = await Comandos.FilasJson(c, tr, """
            SELECT jsonb_build_object('nombre',coalesce(u.nombre_mostrar,concat_ws(' ',p.nombres,p.apellidos)),
                'correo',u.correo_institucional,'alumno',u.id_usuario=@alumno)::text
            FROM public.usuarios u JOIN public.personas p USING(id_persona)
            WHERE u.id_usuario=@alumno OR u.id_usuario=@docente::int
            ORDER BY u.id_usuario=@alumno DESC
            """, ct, ("alumno", alumno), ("docente", docente));
        string nombreAlumno = cuentas[0].GetProperty("nombre").GetString()!;
        string nombreDocente = cuentas.Count > 1 ? cuentas[1].GetProperty("nombre").GetString()! : "Sin docente asignado";
        string contactoDocente = cuentas.Count > 1 ? "\nCorreo del docente: " + cuentas[1].GetProperty("correo").GetString() : "";
        foreach (var cuenta in cuentas)
        {
            await Encolar(c, tr, evento, cuenta.GetProperty("correo").GetString()!, "Asignación de docente actualizada", $"Alumno: {nombreAlumno}\nDocente: {nombreDocente}{contactoDocente}\nAdministración actualizó la asignación. Los próximos avisos del trabajo se enviarán a las personas responsables.\nFecha: {FechaLocal()}", ct);
        }
    }
}
