using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Npgsql;

namespace APIScriptoriumArchivum.Datos;

public sealed class Recuperacion(
    NpgsqlDataSource baseDeDatos, Usuarios usuarios, IDataProtectionProvider proteccion,
    IPasswordHasher<Cuenta> contrasenas, CorreoRecuperacion correo)
{
    private const int Minutos = 10;
    private IDataProtector protector = proteccion.CreateProtector("Scriptorium.Recuperacion.v1");

    public async Task Solicitar(string direccion, CancellationToken ct)
    {
        Cuenta? cuenta = await usuarios.BuscarAsync(null, direccion, ct);
        if (cuenta is null) return;
        string codigo = RandomNumberGenerator.GetInt32(1_000_000).ToString("D6", CultureInfo.InvariantCulture);
        string protegido = protector.Protect(cuenta.VersionSesion + ":" + codigo);
        await using var conexion = await baseDeDatos.OpenConnectionAsync(ct);
        await using var comando = Comandos.Crear(conexion, null, """
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
            """, ("id", cuenta.Id), ("codigo", protegido), ("minutos", Minutos));
        if (await comando.ExecuteNonQueryAsync(ct) == 1)
        {
            correo.Encolar(cuenta.Id, cuenta.Correo, codigo, Minutos);
        }
    }

    public async Task<bool> Confirmar(string direccion, string codigo, string nueva, CancellationToken ct)
    {
        Cuenta? cuenta = await usuarios.BuscarAsync(null, direccion, ct);
        if (cuenta is null) return false;
        await using var conexion = await baseDeDatos.OpenConnectionAsync(ct);
        await using var tr = await conexion.BeginTransactionAsync(ct);
        string? guardado = await Comandos.Valor<string>(conexion, tr, """
            SELECT codigo_protegido FROM public.recuperaciones_contrasena
            WHERE id_usuario = @id AND codigo_protegido IS NOT NULL AND vence_en > clock_timestamp()
              AND intentos < 5 FOR UPDATE
            """, ct, ("id", cuenta.Id));
        if (guardado is null) return false;

        bool valido;
        try
        {
            valido = CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(protector.Unprotect(guardado)),
                Encoding.UTF8.GetBytes(cuenta.VersionSesion + ":" + codigo));
        }
        catch (CryptographicException) { valido = false; }
        if (!valido)
        {
            await Comandos.Ejecutar(conexion, tr, """
                UPDATE public.recuperaciones_contrasena SET intentos = intentos + 1 WHERE id_usuario = @id
                """, ct, ("id", cuenta.Id));
            await tr.CommitAsync(ct);
            return false;
        }

        string nuevo = contrasenas.HashPassword(cuenta, nueva);
        await using var cambio = Comandos.Crear(conexion, tr, """
            UPDATE public.usuarios SET hash_contrasena = @nuevo
            WHERE id_usuario = @id AND hash_contrasena = @anterior AND estado = 'Habilitada'
            """, ("id", cuenta.Id), ("nuevo", nuevo), ("anterior", cuenta.Hash));
        if (await cambio.ExecuteNonQueryAsync(ct) != 1) return false;
        await Comandos.Ejecutar(conexion, tr, """
            UPDATE public.recuperaciones_contrasena SET codigo_protegido = NULL WHERE id_usuario = @id
            """, ct, ("id", cuenta.Id));
        await tr.CommitAsync(ct);
        return true;
    }
}
