using System.Text.Json;
using APIScriptoriumArchivum.Datos;
using Microsoft.AspNetCore.Mvc;

namespace APIScriptoriumArchivum.Controllers;
// Solo se publican las secciones que cambiaron, no el contenido de sus registros
// Cada consulta posterior conserva los controles de acceso de su propia ruta
[ApiController, Route("api/cambios")]
public sealed class CambiosController(CambiosEnVivo cambios) : ControllerBase
{
    // Devuelve los contadores que permiten detectar cambios en las secciones
    [HttpGet("estado")]
    public IActionResult Estado()
    {
        // no-store es una directiva HTTP que impide guardar esta respuesta en cache
        Response.Headers.CacheControl = "no-store";
        return Ok(cambios.Estado());
    }

    // Mantiene una conexion de eventos para avisar a la interfaz cuando hay cambios
    [HttpGet]
    public async Task Escuchar(CancellationToken ct)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-store";
        Response.Headers["X-Accel-Buffering"] = "no";
        var(id, reader) = cambios.Suscribir();
        try
        {
            // retry y data pertenecen al formato de eventos que entiende el navegador
            await Response.WriteAsync("retry: 2000\ndata: [\"todo\"]\n\n", ct);
            await Response.Body.FlushAsync(ct);
            var lectura = reader.ReadAsync(ct).AsTask();
            while (!ct.IsCancellationRequested)
            {
                if (await Task.WhenAny(lectura, Task.Delay(TimeSpan.FromSeconds(15), ct)) == lectura)
                {
                    var areas = await lectura;
                    await Response.WriteAsync("data: " + JsonSerializer.Serialize(areas) + "\n\n", ct);
                    lectura = reader.ReadAsync(ct).AsTask();
                }
                else
                {
                    await Response.WriteAsync(": mantener conexion\n\n", ct);
                }

                await Response.Body.FlushAsync(ct);
            }
        }
        // El cierre de la conexion cancela la espera de eventos
        catch (OperationCanceledException)when (ct.IsCancellationRequested)
        {
        }
        finally
        {
            cambios.Quitar(id);
        }
    }
}
