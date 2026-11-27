using APIScriptoriumArchivum;
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

// Carga la configuracion y prepara los servicios de la API
var builder = WebApplication.CreateBuilder(args);
// Lee el archivo privado cuando la API se ejecuta desde el proyecto
EntornoPrivado.Cargar(builder, args);

// Activa los controladores y utiliza nombres con guion bajo en el JSON
builder.Services.AddControllers().AddJsonOptions(opciones =>
{
    opciones.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
});
// Devuelve los campos invalidos cuando los datos recibidos no cumplen el modelo
builder.Services.Configure<ApiBehaviorOptions>(opciones =>
{
    opciones.InvalidModelStateResponseFactory = contexto =>
    {
        string[] campos = contexto.ModelState.Where(c => c.Value?.Errors.Count > 0).Select(c => c.Key).ToArray();
        return new BadRequestObjectResult(new { error = "Revisa los datos enviados.", fields = campos });
    };
});
// Prepara la descripcion de las rutas y sus datos de entrada y salida
builder.Services.AddOpenApi();
// Registra el proveedor de PostgreSQL que comparte el grupo de conexiones
builder.Services.AddSingleton<NpgsqlDataSource>(servicios =>
{
    // Obtiene la configuracion cargada por la aplicacion
    IConfiguration configuracion = servicios.GetRequiredService<IConfiguration>();
    // Obtiene la cadena y la clave desde la configuracion ya cargada
    string cadena = EntornoPrivado.CadenaConexion(configuracion);
    // Prepara el proveedor y abre conexiones cuando se necesitan
    return NpgsqlDataSource.Create(cadena);
});
// Crea contextos de Entity Framework para cada operacion con la base
builder.Services.AddDbContextFactory<AppDbContext>((servicios, opciones) =>
{
    // Utiliza el proveedor de PostgreSQL registrado con la cadena anterior
    opciones.UseNpgsql(servicios.GetRequiredService<NpgsqlDataSource>());
});
// Registra las consultas de catalogos y las operaciones de cuentas
builder.Services.AddScoped<Catalogos>();
builder.Services.AddScoped<Usuarios>();
// Comparte los servicios de avisos y las actualizaciones entre peticiones
builder.Services.AddSingleton<Avisos>();
builder.Services.AddSingleton<RegistroCorreos>();
builder.Services.AddSingleton<CambiosEnVivo>();
// Prepara la asignacion y la revision de los estudiantes por docente
builder.Services.AddScoped<AsignacionesDocentes>();
builder.Services.AddScoped<RevisionesDocentes>();
// Procesa en segundo plano la cola de correos y sus reintentos
builder.Services.AddHostedService<CorreoEventos>();
// Registra las operaciones de fichas y ejemplares y material complementario
builder.Services.AddScoped<Fichas>();
builder.Services.AddScoped<Ejemplares>();
builder.Services.AddScoped<Complementos>();
// Prepara la consulta del catalogo publico con sus permisos de acceso
builder.Services.AddScoped<ConsultaPublica>();
// Registra la administracion de cuentas y el seguimiento de actividad
builder.Services.AddScoped<GestionRoles>();
builder.Services.AddScoped<Presencia>();
// Prepara los codigos de recuperacion y su servicio de envio de correo
builder.Services.AddScoped<Recuperacion>();
builder.Services.AddSingleton<CorreoRecuperacion>();
builder.Services.AddHostedService(servicios => servicios.GetRequiredService<CorreoRecuperacion>());
// Limita los intentos por direccion IP para las rutas que usan estas politicas
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
// Comparte la validacion de PDF y la gestion de cargas por partes
builder.Services.AddSingleton<ArchivoPdf>();
builder.Services.AddSingleton<CargasArchivos>();
// Ejecuta la limpieza de cargas temporales en segundo plano
builder.Services.AddHostedService(servicios => servicios.GetRequiredService<CargasArchivos>());
// Protege las contrasenas mediante hash y permite comprobarlas al ingresar
builder.Services.AddScoped<IPasswordHasher<Cuenta>, PasswordHasher<Cuenta>>();

// Identifica la cuenta mediante una cookie de sesion
builder.Services.AddAuthentication("Scriptorium").AddCookie("Scriptorium", opciones =>
{
    opciones.Cookie.Name = "Scriptorium.Sesion";
    opciones.Cookie.HttpOnly = true;
    opciones.Cookie.SameSite = SameSiteMode.Strict;
    opciones.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    opciones.ExpireTimeSpan = TimeSpan.FromHours(8);
    // Controla las respuestas de acceso y comprueba que la sesion siga vigente
    opciones.Events = new CookieAuthenticationEvents
    {
        // Responde con estado 401 cuando falta iniciar sesion
        OnRedirectToLogin = contexto =>
        {
            contexto.Response.StatusCode = 401;
            return contexto.Response.WriteAsJsonAsync(new { error = "Inicia sesión." });
        },
        // Responde con estado 403 cuando la cuenta no tiene permiso
        OnRedirectToAccessDenied = contexto =>
        {
            contexto.Response.StatusCode = 403;
            return contexto.Response.WriteAsJsonAsync(new { error = "Tu cuenta no tiene permiso para esta operación." });
        },
        // Consulta la cuenta actual y comprueba el rol y la version de sesion
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
                // Rechaza la cookie si la cuenta o sus datos de sesion dejaron de ser validos
                contexto.RejectPrincipal();
                await contexto.HttpContext.SignOutAsync();
            }
            else
            {
                // Conserva la cuenta consultada durante esta peticion
                contexto.HttpContext.Items[typeof(Cuenta)] = cuenta;
                // Agrega los permisos de los otros roles al Superadministrador sin cambiar su rol principal
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
// Activa la comprobacion de permisos declarados en los controladores
builder.Services.AddAuthorization();

// Construye la aplicacion con los servicios registrados
var app = builder.Build();
// Devuelve los errores previstos y evita mostrar detalles internos de otros fallos
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

// Publica la descripcion OpenAPI solo en el entorno de desarrollo
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
// Activa la redireccion a HTTPS cuando la configuracion lo indica
if (app.Configuration.GetValue("RedireccionHttps", true))
{
    app.UseHttpsRedirection();
}

app.Use(async (contexto, continuar) =>
{
    // Comprueba el origen informado por el navegador antes de permitir modificaciones
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

// Identifica la ruta correspondiente a cada peticion
app.UseRouting();
// Aplica los limites de intentos definidos para la ruta
app.UseRateLimiter();
// Identifica a la cuenta a partir de su cookie
app.UseAuthentication();
// Comprueba si la cuenta puede ejecutar la operacion solicitada
app.UseAuthorization();
app.Use(async (contexto, continuar) =>
{
    await continuar();
    // Notifica los cambios despues de una operacion de escritura que termino correctamente
    if (contexto.Response.StatusCode is >= 200 and < 300 && contexto.Request.Method is "POST" or "PUT" or "PATCH" or "DELETE")
    {
        contexto.RequestServices.GetRequiredService<CambiosEnVivo>().Peticion(contexto.Request.Path.Value?.ToLowerInvariant() ?? "");
    }
});
// Conecta las rutas HTTP con las acciones de los controladores
app.MapControllers();
// Inicia el servidor y espera las peticiones de los clientes
app.Run();
