using System.Security.Claims;
using APIScriptoriumArchivum.Datos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace APIScriptoriumArchivum.Controllers;

[ApiController]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("api/trabajos/{id:int}/archivos")]
public sealed class ArchivosController(NpgsqlDataSource db, ConsultaPublica consulta, Avisos avisos,
    IConfiguration configuracion, IWebHostEnvironment entorno) : ControllerBase
{
    private readonly string carpeta = Path.GetFullPath(configuracion["Storage:Ruta"] ??
        Path.Combine(entorno.ContentRootPath, "storage", "trabajos"));
    private static readonly HashSet<string> Extensiones = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".zip", ".txt", ".csv", ".md",
        ".js", ".ts", ".html", ".css", ".json", ".py", ".java", ".cs", ".sql", ".cpp", ".c", ".h"
    };

    private async Task<bool> PuedeGestionar(int id, CancellationToken ct)
    {
        if (User.IsInRole("Biblioteca")) return true;
        if (!User.IsInRole("Lector") && !User.IsInRole("Estudiante")) return false;
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int actor)) return false;
        await using var c = await db.OpenConnectionAsync(ct);
        return await Comandos.Valor<bool>(c, null,
            "SELECT EXISTS(SELECT 1 FROM public.trabajos WHERE id_trabajo=@id AND id_usuario_envio=@actor)",
            ct, ("id", id), ("actor", actor));
    }

    private async Task<bool> Acceso(int id, CancellationToken ct)
    {
        bool gestion = await PuedeGestionar(id, ct);
        if (!gestion && !(await consulta.Buscar(id, ct)).Descarga)
            throw new ErrorApi(403, "La descarga de archivos complementarios no está autorizada.");
        return gestion;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(int id, CancellationToken ct)
    {
        bool gestion = await Acceso(id, ct);
        await using var c = await db.OpenConnectionAsync(ct);
        return Ok(await Comandos.FilasJson(c, """
            SELECT jsonb_build_object('id',a.id_archivo::text,'nombre',a.nombre,'descripcion',a.descripcion,
                'tamano_bytes',a.tamano_bytes,'habilitado',a.habilitado)::text
            FROM public.archivos_complementarios a JOIN public.trabajos t USING(id_trabajo)
            WHERE a.id_trabajo=@id AND (@gestion OR
                (a.habilitado AND (a.id_entrega IS NULL OR a.id_entrega=t.id_entrega_final)))
            ORDER BY a.fecha_registro,a.id_archivo
            """, ct, ("id", id), ("gestion", gestion)));
    }

    [Authorize(Roles = "Biblioteca,Lector,Estudiante")]
    [HttpPost]
    [RequestSizeLimit(26L * 1024 * 1024)]
    public async Task<IActionResult> Subir(int id, IFormFile? archivo, [FromForm] string? descripcion, CancellationToken ct)
    {
        if (!await PuedeGestionar(id, ct)) throw new ErrorApi(403, "Solo podés adjuntar archivos a tus propios envíos.");
        if (archivo is null || archivo.Length == 0 || archivo.Length > 25L * 1024 * 1024)
            throw new ErrorApi(400, "Seleccioná un archivo de hasta 25 MB.");
        string nombre = Path.GetFileName(archivo.FileName.Replace('\\', '/'));
        string extension = Path.GetExtension(nombre).ToLowerInvariant();
        string detalle = descripcion?.Trim() ?? "";
        if (!Extensiones.Contains(extension) || nombre.Length > 255 || nombre.Any(char.IsControl))
            throw new ErrorApi(400, "Usá PDF, Word, Excel, ZIP o un archivo de código o texto permitido.");
        if (detalle.Length is 0 or > 1500) throw new ErrorApi(400, "Escribí una descripción de hasta 1500 caracteres.");
        Directory.CreateDirectory(carpeta);
        string clave = Guid.NewGuid().ToString("N") + extension;
        string ruta = Path.Combine(carpeta, clave);
        try
        {
            await using (var salida = new FileStream(ruta, FileMode.CreateNew, FileAccess.Write))
                await archivo.CopyToAsync(salida, ct);
            await using var c = await db.OpenConnectionAsync(ct);
            await using var tr = await c.BeginTransactionAsync(ct);
            await Fichas.Bloquear(c, tr, id, ct);
            long nuevo = await Comandos.Valor<long>(c, tr, """
                INSERT INTO public.archivos_complementarios
                    (id_trabajo,id_usuario,id_entrega,nombre,clave,descripcion,tamano_bytes)
                VALUES(@id,@actor,(SELECT id_entrega FROM public.entregas WHERE id_trabajo=@id
                    ORDER BY numero_version DESC LIMIT 1),@nombre,@clave,@descripcion,@bytes)
                RETURNING id_archivo
                """, ct, ("id", id), ("actor", int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!)),
                ("nombre", nombre), ("clave", clave), ("descripcion", detalle), ("bytes", archivo.Length));
            await avisos.Trabajo(c, tr, id, int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),
                "ARCHIVO_ADJUNTADO", "Se agregó material complementario",
                $"Se adjuntó el archivo {nombre}. Biblioteca debe habilitarlo antes de su difusión pública.", !User.IsInRole("Biblioteca"), ct);
            await tr.CommitAsync(CancellationToken.None);
            return StatusCode(201, new { id = nuevo.ToString() });
        }
        catch
        {
            System.IO.File.Delete(ruta);
            throw;
        }
    }

    [Authorize(Roles = "Biblioteca")]
    [HttpPatch("{archivo:long}")]
    public async Task<IActionResult> Habilitar(int id, long archivo, EstadoArchivo datos, CancellationToken ct)
    {
        if (!datos.Habilitado.HasValue) throw new ErrorApi(400, "Indicá si se permite difundir el archivo.");
        await using var c = await db.OpenConnectionAsync(ct);
        await using var tr = await c.BeginTransactionAsync(ct);
        await Fichas.Bloquear(c, tr, id, ct);
        await using var comando = Comandos.Crear(c, tr, """
            UPDATE public.archivos_complementarios SET habilitado=@estado
            WHERE id_trabajo=@id AND id_archivo=@archivo
            """, ("id", id), ("archivo", archivo), ("estado", datos.Habilitado.Value));
        if (await comando.ExecuteNonQueryAsync(ct) != 1) throw new ErrorApi(404, "El archivo no existe.");
        await avisos.Trabajo(c, tr, id, int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),
            "PERMISO_ARCHIVO_ACTUALIZADO", "Permiso de archivo actualizado",
            datos.Habilitado.Value ? "Biblioteca habilitó la difusión de un archivo complementario, según los permisos del trabajo."
                : "Biblioteca deshabilitó la difusión de un archivo complementario.", false, ct);
        await tr.CommitAsync(ct);
        return Ok(new { ok = true });
    }

    [HttpGet("{archivo:long}")]
    public async Task<IActionResult> Descargar(int id, long archivo, CancellationToken ct)
    {
        bool gestion = await Acceso(id, ct);
        await using var c = await db.OpenConnectionAsync(ct);
        await using var comando = Comandos.Crear(c, null, """
            SELECT a.clave,a.nombre FROM public.archivos_complementarios a
            JOIN public.trabajos t USING(id_trabajo)
            WHERE a.id_trabajo=@id AND a.id_archivo=@archivo AND
                (@gestion OR (a.habilitado AND (a.id_entrega IS NULL OR a.id_entrega=t.id_entrega_final)))
            """, ("id", id), ("archivo", archivo), ("gestion", gestion));
        await using var lector = await comando.ExecuteReaderAsync(ct);
        if (!await lector.ReadAsync(ct)) throw new ErrorApi(404, "El archivo no está disponible.");
        string clave = lector.GetString(0);
        if (Path.GetFileName(clave) != clave || !Guid.TryParseExact(Path.GetFileNameWithoutExtension(clave), "N", out _))
            throw new ErrorApi(404, "El archivo no está disponible.");
        string ruta = Path.Combine(carpeta, clave);
        if (!System.IO.File.Exists(ruta)) throw new ErrorApi(404, "El archivo no está disponible.");
        Response.Headers.XContentTypeOptions = "nosniff";
        return PhysicalFile(ruta, "application/octet-stream", lector.GetString(1));
    }

    public record EstadoArchivo(bool? Habilitado);
}
