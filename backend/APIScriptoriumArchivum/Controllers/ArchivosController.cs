using APIScriptoriumArchivum.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Security.Claims;
using APIScriptoriumArchivum.Datos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace APIScriptoriumArchivum.Controllers;
// Gestiona los anexos de los trabajos y comprueba quien puede adjuntarlos o descargarlos
[ApiController]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("api/trabajos/{id:int}/[controller]")]
public sealed class ArchivosController(IDbContextFactory<AppDbContext> db, ConsultaPublica consulta, Avisos avisos, IConfiguration configuracion, IWebHostEnvironment entorno, CargasArchivos cargas) : ControllerBase
{
    // Carpeta fisica donde se guardan los anexos de los trabajos
    private string carpeta = Path.GetFullPath(configuracion["Storage:Ruta"] ?? Path.Combine(entorno.ContentRootPath, "storage", "trabajos"));
    // Comprueba si actua Biblioteca o el usuario que envio el trabajo
    private async Task<bool> PuedeGestionar(int id, CancellationToken ct)
    {
        if (User.IsInRole("Biblioteca"))
        {
            return true;
        }

        if (!User.IsInRole("Lector") && !User.IsInRole("Estudiante"))
        {
            return false;
        }

        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int actor))
        {
            return false;
        }

        await using var c = await db.CreateDbContextAsync(ct);
        return await Comandos.Valor<bool>(c, null, "SELECT EXISTS(SELECT 1 FROM public.trabajos WHERE id_trabajo=@id AND id_usuario_envio=@actor)", ct, ("id", id), ("actor", actor));
    }

    // Valida la gestion del anexo o el permiso publico de descarga del trabajo
    private async Task<bool> Acceso(int id, CancellationToken ct)
    {
        bool gestion = await PuedeGestionar(id, ct);
        if (!gestion && !(await consulta.Buscar(id, ct)).Descarga)
        {
            throw new ErrorApi(403, "La descarga de archivos complementarios no está autorizada.");
        }

        return gestion;
    }

    // Lista los anexos disponibles segun los permisos de quien consulta
    [HttpGet]
    // async permite esperar la consulta y Task representa el resultado pendiente
    public async Task<IActionResult> Listar(int id, CancellationToken ct)
    {
        bool gestion = await Acceso(id, ct);
        await using var c = await db.CreateDbContextAsync(ct);
        return Ok(await Comandos.FilasJson(c, """
            SELECT jsonb_build_object('id',a.id_archivo::text,'nombre',a.nombre,'descripcion',a.descripcion,
                'tamano_bytes',a.tamano_bytes,'habilitado',a.habilitado)::text
            FROM public.archivos_complementarios a JOIN public.trabajos t USING(id_trabajo)
            WHERE a.id_trabajo=@id AND (@gestion OR
                (a.habilitado AND (a.id_entrega IS NULL OR a.id_entrega=t.id_entrega_final)))
            ORDER BY a.fecha_registro,a.id_archivo
            """, ct, ("id", id), ("gestion", gestion)));
    }

    // Guarda el anexo en almacenamiento y registra sus datos y el aviso en la base
    [Authorize(Roles = "Biblioteca,Lector,Estudiante")]
    [HttpPost]
    [RequestSizeLimit(CargasArchivos.MaximoBytes + 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = CargasArchivos.MaximoBytes)]
    public async Task<IActionResult> Subir(int id, IFormFile? archivo, [FromForm] string? descripcion, [FromForm(Name = "carga_id")] string? cargaId, CancellationToken ct)
    {
        if (!await PuedeGestionar(id, ct))
        {
            throw new ErrorApi(403, "Solo puedes adjuntar archivos a tus propios envíos.");
        }

        await using var recibido = await cargas.Resolver(archivo, cargaId, int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), ct);
        archivo = recibido.Archivo;
        if (archivo is null || archivo.Length == 0)
        {
            throw new ErrorApi(400, "Selecciona un archivo con contenido.");
        }

        string nombre = Path.GetFileName(archivo.FileName.Replace('\\', '/'));
        string detalle = descripcion?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(nombre) || nombre is "." or ".." || nombre.Length > 255 || nombre.Any(char.IsControl))
        {
            throw new ErrorApi(400, "El nombre del archivo no es válido.");
        }

        if (detalle.Length is 0 or > 1500)
        {
            throw new ErrorApi(400, "Escribe una descripción de hasta 1500 caracteres.");
        }

        Directory.CreateDirectory(carpeta);
        string clave = Guid.NewGuid().ToString("N") + ".bin";
        string ruta = Path.Combine(carpeta, clave);
        try
        {
            await using (var salida = new FileStream(ruta, FileMode.CreateNew, FileAccess.Write))
            {
                await archivo.CopyToAsync(salida, ct);
            }

            await using var c = await db.CreateDbContextAsync(ct);
            await using var tr = await c.Database.BeginTransactionAsync(ct);
            await Fichas.Bloquear(c, tr, id, ct);
            int? entrega = await c.Entregas.Where(e => e.IdTrabajo == id).OrderByDescending(e => e.NumeroVersion).Select(e => (int? )e.IdEntrega).FirstOrDefaultAsync(ct);
            var adjunto = new APIScriptoriumArchivum.Models.ArchivosComplementarios
            {
                IdTrabajo = id,
                IdUsuario = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),
                IdEntrega = entrega,
                Nombre = nombre,
                Clave = clave,
                Descripcion = detalle,
                TamanoBytes = archivo.Length
            };
            c.ArchivosComplementarios.Add(adjunto);
            await c.SaveChangesAsync(ct);
            long nuevo = adjunto.IdArchivo;
            await avisos.Trabajo(c, tr, id, int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), "ARCHIVO_ADJUNTADO", "Se agregó material complementario", $"Se adjuntó el archivo {nombre}. Biblioteca debe habilitarlo antes de su difusión pública.", !User.IsInRole("Biblioteca"), ct);
            await tr.CommitAsync(CancellationToken.None);
            return StatusCode(201, new { id = nuevo.ToString() });
        }
        catch
        {
            System.IO.File.Delete(ruta);
            throw;
        }
    }

    // Permite a Biblioteca habilitar o deshabilitar la difusion de un anexo
    [Authorize(Roles = "Biblioteca")]
    [HttpPatch("{archivo:long}")]
    public async Task<IActionResult> Habilitar(int id, long archivo, EstadoArchivo datos, CancellationToken ct)
    {
        if (!datos.Habilitado.HasValue)
        {
            throw new ErrorApi(400, "Indica si se permite difundir el archivo.");
        }

        await using var c = await db.CreateDbContextAsync(ct);
        await using var tr = await c.Database.BeginTransactionAsync(ct);
        await Fichas.Bloquear(c, tr, id, ct);
        int modificados = await c.ArchivosComplementarios.Where(a => a.IdTrabajo == id && a.IdArchivo == archivo).ExecuteUpdateAsync(s => s.SetProperty(a => a.Habilitado, datos.Habilitado.Value), ct);
        if (modificados != 1)
        {
            throw new ErrorApi(404, "El archivo no existe.");
        }

        await avisos.Trabajo(c, tr, id, int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), "PERMISO_ARCHIVO_ACTUALIZADO", "Permiso de archivo actualizado", datos.Habilitado.Value ? "Biblioteca habilitó la difusión de un archivo complementario, según los permisos del trabajo." : "Biblioteca deshabilitó la difusión de un archivo complementario.", false, ct);
        await tr.CommitAsync(ct);
        return Ok(new { ok = true });
    }

    // Entrega el archivo despues de revisar su permiso y existencia
    [HttpGet("{archivo:long}")]
    public async Task<IActionResult> Descargar(int id, long archivo, CancellationToken ct)
    {
        bool gestion = await Acceso(id, ct);
        await using var c = await db.CreateDbContextAsync(ct);
        var adjunto = await (
            from a in c.ArchivosComplementarios.AsNoTracking()join t in c.Trabajos on a.IdTrabajo equals t.IdTrabajo
            where a.IdTrabajo == id && a.IdArchivo == archivo && (gestion || (a.Habilitado && (a.IdEntrega == null || a.IdEntrega == t.IdEntregaFinal)))select new
            {
                a.Clave,
                a.Nombre
            }

        ).SingleOrDefaultAsync(ct);
        if (adjunto is null)
        {
            throw new ErrorApi(404, "El archivo no está disponible.");
        }

        string clave = adjunto.Clave;
        if (Path.GetFileName(clave) != clave || !Guid.TryParseExact(Path.GetFileNameWithoutExtension(clave), "N", out _))
        {
            throw new ErrorApi(404, "El archivo no está disponible.");
        }

        string ruta = Path.Combine(carpeta, clave);
        if (!System.IO.File.Exists(ruta))
        {
            throw new ErrorApi(404, "El archivo no está disponible.");
        }

        Response.Headers.XContentTypeOptions = "nosniff";
        return PhysicalFile(ruta, "application/octet-stream", adjunto.Nombre, enableRangeProcessing: true);
    }

    // Indica si Biblioteca permite difundir un anexo
    public record EstadoArchivo(bool? Habilitado);
}
