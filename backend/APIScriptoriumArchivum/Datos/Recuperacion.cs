using APIScriptoriumArchivum.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Npgsql;

namespace APIScriptoriumArchivum.Datos;
public sealed class Recuperacion(IDbContextFactory<AppDbContext> baseDeDatos, Usuarios usuarios, IDataProtectionProvider proteccion, IPasswordHasher<Cuenta> contrasenas, CorreoRecuperacion correo, ILogger<Recuperacion> log)
{
    private const int Minutos = 10;
    private IDataProtector protector = proteccion.CreateProtector("Scriptorium.Recuperacion.v1");
    public async Task Solicitar(string direccion, CancellationToken ct, string? solicitudId = null)
    {
        Cuenta? cuenta = await usuarios.BuscarAsync(null, direccion, ct);
        if (cuenta is null)
        {
            log.LogInformation("Solicitud de recuperación {Solicitud}: sin cuenta habilitada", solicitudId);
            return;
        }

        string codigo = RandomNumberGenerator.GetInt32(1_000_000).ToString("D6", CultureInfo.InvariantCulture);
        string protegido = protector.Protect(cuenta.VersionSesion + ":" + codigo);
        await using var conexion = await baseDeDatos.CreateDbContextAsync(ct);
        int modificados = await Comandos.Ejecutar(conexion, null, """
            INSERT INTO public.recuperaciones_contrasena
                (id_usuario, codigo_protegido, vence_en, ultimo_envio, inicio_ventana)
            VALUES (@id, @codigo, now() + make_interval(mins => @minutos), now(), now())
            ON CONFLICT (id_usuario) DO UPDATE SET codigo_protegido = excluded.codigo_protegido,
                vence_en = excluded.vence_en, ultimo_envio = now(), intentos = 0,
                inicio_ventana = CASE WHEN recuperaciones_contrasena.inicio_ventana <= now() - interval '1 hour'
                    THEN now() ELSE recuperaciones_contrasena.inicio_ventana END,
                envios_ventana = CASE WHEN recuperaciones_contrasena.inicio_ventana <= now() - interval '1 hour'
                    THEN 1 ELSE recuperaciones_contrasena.envios_ventana + 1 END
            WHERE recuperaciones_contrasena.ultimo_envio <= now() - interval '60 seconds'
              AND (recuperaciones_contrasena.inicio_ventana <= now() - interval '1 hour'
                   OR recuperaciones_contrasena.envios_ventana < 5)
            """, ct, ("id", cuenta.Id), ("codigo", protegido), ("minutos", Minutos));
        if (modificados == 1)
        {
            correo.Encolar(cuenta.Id, cuenta.Correo, codigo, Minutos);
            log.LogInformation("Solicitud de recuperación {Solicitud}: encolada para usuario {Usuario}", solicitudId, cuenta.Id);
        }
        else
        {
            log.LogInformation("Solicitud de recuperación {Solicitud}: limitada para usuario {Usuario}", solicitudId, cuenta.Id);
        }
    }

    public async Task<bool> Confirmar(string direccion, string codigo, string nueva, CancellationToken ct)
    {
        Cuenta? cuenta = await usuarios.BuscarAsync(null, direccion, ct);
        if (cuenta is null)
        {
            return false;
        }

        await using var conexion = await baseDeDatos.CreateDbContextAsync(ct);
        await using var tr = await conexion.Database.BeginTransactionAsync(ct);
        var solicitudes = await conexion.RecuperacionesContrasena.FromSqlRaw("""
            SELECT * FROM public.recuperaciones_contrasena
            WHERE id_usuario = @id AND codigo_protegido IS NOT NULL AND vence_en > clock_timestamp()
              AND intentos < 5 FOR UPDATE
            """, Comandos.Parametros(("id", cuenta.Id))).ToListAsync(ct);
        var solicitud = solicitudes.SingleOrDefault();
        if (solicitud is null)
        {
            return false;
        }

        string guardado = solicitud.CodigoProtegido!;
        bool valido;
        try
        {
            valido = CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(protector.Unprotect(guardado)), Encoding.UTF8.GetBytes(cuenta.VersionSesion + ":" + codigo));
        }
        catch (CryptographicException)
        {
            valido = false;
        }

        if (!valido)
        {
            solicitud.Intentos++;
            await conexion.SaveChangesAsync(ct);
            await tr.CommitAsync(ct);
            return false;
        }

        string nuevo = contrasenas.HashPassword(cuenta, nueva);
        int cambiados = await conexion.Usuarios.Where(u => u.IdUsuario == cuenta.Id && u.HashContrasena == cuenta.Hash && u.Estado == "Habilitada").ExecuteUpdateAsync(s => s.SetProperty(u => u.HashContrasena, nuevo), ct);
        if (cambiados != 1)
        {
            return false;
        }

        solicitud.CodigoProtegido = null;
        await conexion.SaveChangesAsync(ct);
        await tr.CommitAsync(ct);
        return true;
    }
}
