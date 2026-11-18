using System.Security.Claims;
using System.Net.Mail;
using APIScriptoriumArchivum.Datos;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace APIScriptoriumArchivum.Controllers
{
    [ApiController]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly Usuarios usuarios;
        private readonly Avisos avisos;
        private readonly IPasswordHasher<Cuenta> contrasenas;

        public AuthController(Usuarios usuarios, IPasswordHasher<Cuenta> contrasenas, Avisos avisos)
        {
            this.usuarios = usuarios;
            this.avisos = avisos;
            this.contrasenas = contrasenas;
        }

        [HttpPost("login")]
        [EnableRateLimiting("cuentas")]
        public async Task<IActionResult> Entrar([FromBody] InicioSesion solicitud, CancellationToken ct)
        {
            string correo = solicitud.Correo?.Trim().ToLowerInvariant() ?? "";
            if (correo.Length is 0 or > 150 || string.IsNullOrEmpty(solicitud.Password) || solicitud.Password.Length > 512)
            {
                return Unauthorized(new { error = "Correo o contraseña incorrectos." });
            }

            Cuenta? cuenta = await usuarios.BuscarAsync(null, correo, ct);
            if (cuenta is null ||
                contrasenas.VerifyHashedPassword(cuenta, cuenta.Hash, solicitud.Password) == PasswordVerificationResult.Failed)
            {
                return Unauthorized(new { error = "Correo o contraseña incorrectos." });
            }

            await avisos.Acceso(cuenta.Id, "INICIO_SESION", ct);
            await AbrirSesion(cuenta);
            return Ok(Perfil(cuenta));
        }

        [HttpPost("registro")]
        [EnableRateLimiting("cuentas")]
        public async Task<IActionResult> Registrar(RegistroLector datos, CancellationToken ct)
        {
            string nombres = datos.Nombres?.Trim() ?? "", apellidos = datos.Apellidos?.Trim() ?? "";
            string correo = datos.Correo?.Trim().ToLowerInvariant() ?? "";
            if (nombres.Length is 0 or > 100 || apellidos.Length is 0 or > 100 ||
                nombres.Any(char.IsControl) || apellidos.Any(char.IsControl))
                return BadRequest(new { error = "Escribí tus nombres y apellidos." });
            if (correo.Length > 150 || !MailAddress.TryCreate(correo, out var direccion) || direccion.Address != correo)
                return BadRequest(new { error = "Ingresá un correo válido." });
            if (datos.Password is not { Length: >= 8 and <= 512 })
                return BadRequest(new { error = "Usá una contraseña de entre 8 y 512 caracteres." });

            string hash = contrasenas.HashPassword(new Cuenta(), datos.Password);
            int id = await usuarios.RegistrarLectorAsync(nombres, apellidos, correo, hash, ct, datos.IdCarrera, datos.IdPlan);
            Cuenta cuenta = (await usuarios.BuscarAsync(id, null, ct))!;
            await AbrirSesion(cuenta);
            return StatusCode(201, Perfil(cuenta));
        }

        [Authorize]
        [HttpGet("me")]
        public IActionResult Sesion() => Ok(Perfil(CuentaActual()));

        [Authorize]
        [HttpPut("perfil")]
        public async Task<IActionResult> ActualizarPerfil(PerfilDatos datos, CancellationToken ct)
        {
            string? nombre = datos.Nombre?.Trim();
            if (nombre is not null && (nombre.Length is 0 or > 120 || nombre.Any(char.IsControl)))
                return BadRequest(new { error = "Ingresá un nombre visible de hasta 120 caracteres." });
            if (datos.Avatar is not null and not ("turquesa" or "violeta") ||
                datos.Tema is not null and not ("sistema" or "claro" or "oscuro"))
                return BadRequest(new { error = "Elegí un avatar y un tema disponibles." });
            int id = CuentaActual().Id;
            await usuarios.ActualizarPerfilAsync(id, nombre, datos.Avatar, datos.Tema, ct, datos.IdCarrera, datos.IdPlan);
            Cuenta actualizada = (await usuarios.BuscarAsync(id, null, ct))!;
            if (actualizada.VersionSesion != CuentaActual().VersionSesion) await AbrirSesion(actualizada);
            return Ok(Perfil(actualizada));
        }

        [Authorize]
        [HttpPost("foto")]
        [RequestSizeLimit(3L * 1024 * 1024)]
        public async Task<IActionResult> SubirFoto(IFormFile? archivo, CancellationToken ct)
        {
            if (archivo is null || archivo.Length < 12 || archivo.Length > 2L * 1024 * 1024)
                return BadRequest(new { error = "Seleccioná una imagen JPG o PNG de hasta 2 MB." });
            using var memoria = new MemoryStream();
            await archivo.CopyToAsync(memoria, ct);
            byte[] datos = memoria.ToArray();
            string? tipo = datos.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })
                ? "image/png" : datos[0] == 255 && datos[1] == 216 && datos[2] == 255 ? "image/jpeg" : null;
            if (tipo is null) return BadRequest(new { error = "El archivo debe ser una imagen JPG o PNG." });
            int id = CuentaActual().Id;
            await usuarios.GuardarFoto(id, datos, tipo, ct);
            return Ok(Perfil((await usuarios.BuscarAsync(id, null, ct))!));
        }

        [Authorize]
        [HttpGet("foto")]
        public async Task<IActionResult> Foto(CancellationToken ct)
        {
            var foto = await usuarios.LeerFoto(CuentaActual().Id, ct);
            Response.Headers.XContentTypeOptions = "nosniff";
            return foto is { } imagen ? File(imagen.Datos, imagen.Tipo) : NotFound();
        }

        [Authorize]
        [HttpDelete("foto")]
        public async Task<IActionResult> QuitarFoto(CancellationToken ct)
        {
            int id = CuentaActual().Id;
            await usuarios.GuardarFoto(id, null, null, ct);
            return Ok(Perfil((await usuarios.BuscarAsync(id, null, ct))!));
        }

        [Authorize]
        [HttpPut("contrasena")]
        public async Task<IActionResult> CambiarContrasena(CambioContrasena datos, CancellationToken ct)
        {
            Cuenta cuenta = CuentaActual();
            if (datos.Actual is not { Length: > 0 and <= 512 } ||
                contrasenas.VerifyHashedPassword(cuenta, cuenta.Hash, datos.Actual) == PasswordVerificationResult.Failed)
                return BadRequest(new { error = "La contraseña actual es incorrecta." });
            if (datos.Nueva is not { Length: >= 8 and <= 512 } || datos.Nueva == datos.Actual)
                return BadRequest(new { error = "Elegí una contraseña distinta de entre 8 y 512 caracteres." });

            string hash = contrasenas.HashPassword(cuenta, datos.Nueva);
            await usuarios.CambiarContrasenaAsync(cuenta.Id, cuenta.Hash, hash, ct);
            await AbrirSesion((await usuarios.BuscarAsync(cuenta.Id, null, ct))!);
            return Ok(new { ok = true });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Salir(CancellationToken ct)
        {
            if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int id))
                await avisos.Acceso(id, "CIERRE_SESION", ct);
            await HttpContext.SignOutAsync();
            return Ok(new { ok = true });
        }

        private Cuenta CuentaActual() => (Cuenta)HttpContext.Items[typeof(Cuenta)]!;

        private static object Perfil(Cuenta cuenta) => new
        {
            id = cuenta.Id.ToString(), nombre = cuenta.Nombre, correo = cuenta.Correo, rol = cuenta.Rol,
            avatar = cuenta.Avatar, tema = cuenta.Tema,
            id_carrera = cuenta.IdCarrera,
            id_plan = cuenta.IdPlan,
            foto = cuenta.FotoVersion is null ? null : "/api/auth/foto?v=" + cuenta.FotoVersion
        };

        private Task AbrirSesion(Cuenta cuenta)
        {
            Claim[] datos =
            {
                new(ClaimTypes.NameIdentifier, cuenta.Id.ToString()),
                new(ClaimTypes.Name, cuenta.Nombre),
                new(ClaimTypes.Email, cuenta.Correo),
                new(ClaimTypes.Role, cuenta.Rol),
                new("version_sesion", cuenta.VersionSesion)
            };
            return HttpContext.SignInAsync(new ClaimsPrincipal(new ClaimsIdentity(datos, "Scriptorium")),
                new AuthenticationProperties { IsPersistent = true });
        }
    }

    public class InicioSesion
    {
        public string? Correo { get; init; }
        public string? Password { get; init; }
    }

    public record RegistroLector(string? Nombres, string? Apellidos, string? Correo, string? Password, int? IdCarrera = null, int? IdPlan = null);
    public record PerfilDatos(string? Nombre, string? Avatar, string? Tema, int? IdCarrera = null, int? IdPlan = null);
    public record CambioContrasena(string? Actual, string? Nueva);
}
