using System.Text.Json;
using APIScriptoriumArchivum.Datos;
using Microsoft.AspNetCore.Mvc;

namespace APIScriptoriumArchivum.Controllers;

// Only invalidation categories are public. Every refresh still uses its authenticated API.
[ApiController,Route("api/cambios")]
public sealed class CambiosController(CambiosEnVivo cambios):ControllerBase
{
    [HttpGet("estado")]
    public IActionResult Estado(){Response.Headers.CacheControl="no-store";return Ok(cambios.Estado());}

    [HttpGet]
    public async Task Escuchar(CancellationToken ct)
    {
        Response.ContentType="text/event-stream";
        Response.Headers.CacheControl="no-store";
        Response.Headers["X-Accel-Buffering"]="no";
        var (id,reader)=cambios.Suscribir();
        try
        {
            await Response.WriteAsync("retry: 2000\ndata: [\"todo\"]\n\n",ct);
            await Response.Body.FlushAsync(ct);
            var lectura=reader.ReadAsync(ct).AsTask();
            while(!ct.IsCancellationRequested)
            {
                if(await Task.WhenAny(lectura,Task.Delay(TimeSpan.FromSeconds(15),ct))==lectura)
                {
                    var areas=await lectura;
                    await Response.WriteAsync("data: "+JsonSerializer.Serialize(areas)+"\n\n",ct);
                    lectura=reader.ReadAsync(ct).AsTask();
                }
                else await Response.WriteAsync(": mantener conexion\n\n",ct);
                await Response.Body.FlushAsync(ct);
            }
        }
        catch(OperationCanceledException) when(ct.IsCancellationRequested){}
        finally{cambios.Quitar(id);}
    }
}
