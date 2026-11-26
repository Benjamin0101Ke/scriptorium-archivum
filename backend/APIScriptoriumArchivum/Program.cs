using System.Security.Claims;
using System.Text.Json;
using System.Threading.RateLimiting;
using APIScriptoriumArchivum.Datos;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using APIScriptoriumArchivum.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(opciones =>
{
    opciones.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
});
builder.Services.Configure<ApiBehaviorOptions>(opciones =>
{
    opciones.InvalidModelStateResponseFactory = contexto =>
    {
        string[] campos = contexto.ModelState.Where(c => c.Value?.Errors.Count > 0).Select(c => c.Key).ToArray();
        return new BadRequestObjectResult(new { error = "Revisa los datos enviados.", fields = campos });
    };
});
builder.Services.AddOpenApi();
builder.Services.AddSingleton<NpgsqlDataSource>(servicios =>
{
    IConfiguration configuracion = servicios.GetRequiredService<IConfiguration>();
    string? cadena = configuracion.GetConnectionString("Scriptorium");
    if (string.IsNullOrWhiteSpace(cadena))
    {
        throw new InvalidOperationException("Falta configurar ConnectionStrings:Scriptorium.");
    }
    return NpgsqlDataSource.Create(cadena);
});
builder.Services.AddDbContextFactory<AppDbContext>((servicios, opciones) =>
{
    opciones.UseNpgsql(servicios.GetRequiredService<NpgsqlDataSource>());
});
builder.Services.AddScoped<Catalogos>();
builder.Services.AddScoped<Usuarios>();
builder.Services.AddSingleton<Avisos>();
builder.Services.AddSingleton<RegistroCorreos>();
builder.Services.AddSingleton<CambiosEnVivo>();
builder.Services.AddScoped<AsignacionesDocentes>();
builder.Services.AddScoped<RevisionesDocentes>();
builder.Services.AddHostedService<CorreoEventos>();
builder.Services.AddScoped<Fichas>();
builder.Services.AddScoped<Ejemplares>();
builder.Services.AddScoped<Complementos>();
builder.Services.AddScoped<ConsultaPublica>();
builder.Services.AddScoped<GestionRoles>();
builder.Services.AddScoped<Presencia>();
builder.Services.AddScoped<Recuperacion>();
builder.Services.AddSingleton<CorreoRecuperacion>();
builder.Services.AddHostedService(servicios => servicios.GetRequiredService<CorreoRecuperacion>());
builder.Services.AddRateLimiter(opciones =>
{
    opciones.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    opciones.AddPolicy("cuentas", contexto => RateLimitPartition.GetFixedWindowLimiter(
        contexto.Connection.RemoteIpAddress?.ToString() ?? "local",
        _ => new FixedWindowRateLimiterOptions
        { PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    opciones.AddPolicy("recuperacion", contexto => RateLimitPartition.GetFixedWindowLimiter(
        contexto.Connection.RemoteIpAddress?.ToString() ?? "local",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0
        }));
    opciones.OnRejected = (contexto, ct) => new ValueTask(
        contexto.HttpContext.Response.WriteAsJsonAsync(new { error = "Espera un minuto antes de volver a intentar." }, ct));
});
builder.Services.AddSingleton<ArchivoPdf>();
builder.Services.AddSingleton<CargasArchivos>();
builder.Services.AddHostedService(servicios => servicios.GetRequiredService<CargasArchivos>());
builder.Services.AddScoped<IPasswordHasher<Cuenta>, PasswordHasher<Cuenta>>();

builder.Services.AddAuthentication("Scriptorium").AddCookie("Scriptorium", opciones =>
{
    opciones.Cookie.Name = "Scriptorium.Sesion";
    opciones.Cookie.HttpOnly = true;
    opciones.Cookie.SameSite = SameSiteMode.Strict;
    opciones.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    opciones.ExpireTimeSpan = TimeSpan.FromHours(8);
    opciones.Events = new CookieAuthenticationEvents
    {
        OnRedirectToLogin = contexto =>
        {
            contexto.Response.StatusCode = 401;
            return contexto.Response.WriteAsJsonAsync(new { error = "Inicia sesión." });
        },
        OnRedirectToAccessDenied = contexto =>
        {
            contexto.Response.StatusCode = 403;
            return contexto.Response.WriteAsJsonAsync(new { error = "Tu cuenta no tiene permiso para esta operación." });
        },
        OnValidatePrincipal = async contexto =>
        {
            string? idTexto = contexto.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            Cuenta? cuenta = null;
            if (int.TryParse(idTexto, out int id))
            {
                Usuarios usuarios = contexto.HttpContext.RequestServices.GetRequiredService<Usuarios>();
                cuenta = await usuarios.BuscarAsync(id, null, contexto.HttpContext.RequestAborted);
            }
            if (cuenta is null || cuenta.Rol != contexto.Principal?.FindFirstValue(ClaimTypes.Role) ||
                cuenta.VersionSesion != contexto.Principal?.FindFirstValue("version_sesion"))
            {
                contexto.RejectPrincipal();
                await contexto.HttpContext.SignOutAsync();
            }
            else
            {
                contexto.HttpContext.Items[typeof(Cuenta)] = cuenta;
                // El rol principal se conserva para validar la cookie y la versión de sesión.
                if (cuenta.Rol == "Superadministrador" && contexto.Principal?.Identity is ClaimsIdentity identidad)
                {
                    foreach (string rol in new[] { "Administrador", "Biblioteca", "Docente revisor", "Estudiante", "Lector" })
                    {
                        if (!contexto.Principal.IsInRole(rol))
                        {
                            identidad.AddClaim(new Claim(ClaimTypes.Role, rol));
                        }
                    }
                }
            }
        }
    };
});
builder.Services.AddAuthorization();

var app = builder.Build();
app.UseExceptionHandler(new ExceptionHandlerOptions
{
    AllowStatusCode404Response = true,
    ExceptionHandler = async contexto =>
    {
        Exception? error = contexto.Features.Get<IExceptionHandlerFeature>()?.Error;
        if (error is ErrorApi conocido)
        {
            contexto.Response.StatusCode = conocido.Estado;
            await contexto.Response.WriteAsJsonAsync(conocido.Respuesta);
            return;
        }
        contexto.Response.StatusCode = 500;
        await contexto.Response.WriteAsJsonAsync(new { error = "No fue posible completar la operación." });
    }
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
if (app.Configuration.GetValue("RedireccionHttps", true))
{
    app.UseHttpsRedirection();
}

app.Use(async (contexto, continuar) =>
{
    string sitio = contexto.Request.Headers["Sec-Fetch-Site"].ToString();
    string metodo = contexto.Request.Method;
    if (contexto.Request.Path.StartsWithSegments("/api") &&
        sitio is "cross-site" or "same-site" &&
        metodo is not ("GET" or "HEAD" or "OPTIONS"))
    {
        contexto.Response.StatusCode = 403;
        await contexto.Response.WriteAsJsonAsync(new { error = "La operación debe enviarse desde la aplicación." });
        return;
    }
    await continuar(contexto);
});

app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.Use(async (contexto, continuar) =>
{
    await continuar();
    if (contexto.Response.StatusCode is >= 200 and < 300 && contexto.Request.Method is "POST" or "PUT" or "PATCH" or "DELETE")
    {
        contexto.RequestServices.GetRequiredService<CambiosEnVivo>().Peticion(contexto.Request.Path.Value?.ToLowerInvariant() ?? "");
    }
});
app.MapControllers();
app.Run();
