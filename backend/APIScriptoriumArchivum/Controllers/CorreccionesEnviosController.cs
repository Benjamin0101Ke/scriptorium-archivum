using System.Security.Claims;
using System.Text.Json;
using APIScriptoriumArchivum.Datos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace APIScriptoriumArchivum.Controllers;

[ApiController]
[Authorize(Roles = "Lector,Estudiante")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("api/mis-envios/{id:int}")]
public sealed class CorreccionesEnviosController(NpgsqlDataSource db, ArchivoPdf archivos,
    CargasArchivos cargas, Avisos avisos) : ControllerBase
{
    private int Actor() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private static async Task<JsonElement> Estado(NpgsqlConnection c, NpgsqlTransaction? tr,
        int id, int actor, CancellationToken ct)
    {
        var filas = await Comandos.FilasJson(c, tr, """
            SELECT jsonb_build_object(
              'ejemplar_id',e.id_entrega::text,'version',e.numero_version,
              'es_software',lower(btrim(tt.nombre))='proyecto de software',
              'constancia_revocada',coalesce(a.estado='Revocada',false),
              'decision',b.resultado,'motivo_biblioteca',b.motivo_decision,
              'revision_docente',d.resultado,'motivo_docente',d.motivo_decision,
              'puede_reemplazar',t.estado_publicacion='En revisión' AND e.id_entrega IS NOT NULL
                AND (coalesce(b.resultado='Desfavorable',false)
                  OR coalesce(d.resultado='Requiere cambios',false)
                  OR coalesce(a.estado='Revocada',false)))::text
            FROM public.trabajos t JOIN public.tipos_trabajo tt USING(id_tipo_trabajo)
            LEFT JOIN LATERAL (SELECT * FROM public.entregas e WHERE e.id_trabajo=t.id_trabajo
                ORDER BY e.numero_version DESC LIMIT 1) e ON true
            LEFT JOIN LATERAL (SELECT resultado,motivo_decision FROM public.revisiones r
                WHERE r.id_entrega=e.id_entrega AND r.ambito_revision='Difusión' AND r.fecha_cierre IS NOT NULL
                ORDER BY r.fecha_cierre DESC,r.id_revision DESC LIMIT 1) b ON true
            LEFT JOIN LATERAL (SELECT resultado,motivo_decision FROM public.revisiones r
                WHERE r.id_entrega=e.id_entrega AND r.ambito_revision='Revisión docente' AND r.fecha_cierre IS NOT NULL
                ORDER BY r.fecha_cierre DESC,r.id_revision DESC LIMIT 1) d ON true
            LEFT JOIN LATERAL (SELECT estado FROM public.autorizaciones_difusion a
                WHERE a.id_entrega=e.id_entrega ORDER BY a.id_autorizacion DESC LIMIT 1) a ON true
            WHERE t.id_trabajo=@id AND t.id_usuario_envio=@actor
            """, ct, ("id", id), ("actor", actor));
        if (filas.Count == 0) throw new ErrorApi(404, "El envío no existe o no pertenece a tu cuenta.");
        return filas[0];
    }

    [HttpGet("correccion")]
    public async Task<IActionResult> Consultar(int id, CancellationToken ct)
    {
        await using var c = await db.OpenConnectionAsync(ct);
        return Ok(await Estado(c, null, id, Actor(), ct));
    }

    private static void ValidarEstado(JsonElement estado, string? esperado)
    {
        if (!estado.GetProperty("puede_reemplazar").GetBoolean())
            throw new ErrorApi(409, "Solo puedes enviar una nueva versión cuando el trabajo está en revisión y tiene cambios solicitados, una decisión desfavorable o una constancia revocada.");
        if (string.IsNullOrWhiteSpace(esperado) || estado.GetProperty("ejemplar_id").GetString() != esperado)
            throw new ErrorApi(409, "El ejemplar cambió. Actualiza el envío antes de enviar otra versión.");
    }

    [HttpPost("reemplazo")]
    [RequestSizeLimit(CargasArchivos.MaximoBytes + 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = CargasArchivos.MaximoBytes)]
    public async Task<IActionResult> Reemplazar(int id, [FromForm] CorreccionEnvio datos, CancellationToken ct)
    {
        int actor = Actor();
        JsonElement estado;
        await using (var c = await db.OpenConnectionAsync(ct))
            estado = await Estado(c, null, id, actor, ct);
        ValidarEstado(estado, datos.EjemplarId);
        string motivo = datos.Cambios?.Trim() ?? "";
        if (motivo.Length is < 10 or > 1500 || motivo.Any(ch => char.IsControl(ch) && ch is not ('\r' or '\n' or '\t')))
            throw new ErrorApi(400, "Describe los cambios realizados, entre 10 y 1500 caracteres.");

        await using var recibido = await cargas.Resolver(datos.Archivo, datos.CargaId, actor, ct);
        IFormFile? archivo = recibido.Archivo;
        bool software = estado.GetProperty("es_software").GetBoolean();
        string? enlace = datos.Enlace?.Trim();
        if (software)
        {
            if (archivo is not null || enlace is null || enlace.Length > 2048 ||
                !Uri.TryCreate(enlace, UriKind.Absolute, out var url) || url.Scheme != Uri.UriSchemeHttps ||
                string.IsNullOrWhiteSpace(url.Host) || !string.IsNullOrEmpty(url.UserInfo))
                throw new ErrorApi(400, "Proporciona un enlace HTTPS del proyecto, sin credenciales ni archivo adjunto.");
        }
        else if (!string.IsNullOrEmpty(enlace) || archivo is null || archivo.Length < 5 ||
            archivo.Length > CargasArchivos.MaximoBytes || !archivo.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            throw new ErrorApi(400, "Selecciona el PDF corregido, de hasta 6 GB.");

        string? clave = null;
        try
        {
            if (archivo is not null)
            {
                try { clave = await archivos.Guardar(archivo, ct); }
                catch (InvalidDataException) { throw new ErrorApi(400, "Selecciona un PDF válido que se pueda abrir."); }
            }
            await using var c = await db.OpenConnectionAsync(ct);
            await using var tr = await c.BeginTransactionAsync(ct);
            await Fichas.Bloquear(c, tr, id, ct);
            ValidarEstado(await Estado(c, tr, id, actor, ct), datos.EjemplarId);

            string nombre = software ? "Repositorio del proyecto corregido" : Path.GetFileName(archivo!.FileName.Replace('\\','/'));
            if (nombre.Length > 255 || nombre.Any(char.IsControl)) nombre = "ejemplar-corregido.pdf";
            int entrega;
            if (software)
            {
                entrega = await Comandos.Valor<int>(c, tr, """
                    INSERT INTO public.entregas(id_trabajo,numero_version,id_usuario_envia)
                    VALUES(@id,(SELECT coalesce(max(numero_version),0)+1 FROM public.entregas WHERE id_trabajo=@id),@actor)
                    RETURNING id_entrega
                    """, ct, ("id", id), ("actor", actor));
                await Comandos.Ejecutar(c, tr, """
                    INSERT INTO public.recursos_digitales(id_trabajo,id_entrega,clase,ubicacion,nombre,tipo_formato,descripcion)
                    VALUES(@id,@e,'ENLACE',@url,@nombre,'text/html','Repositorio externo del proyecto de software corregido')
                    """, ct, ("id", id), ("e", entrega), ("url", enlace), ("nombre", nombre));
            }
            else
                entrega = await Ejemplares.AgregarVersion(c, tr, id, actor, "PDF", clave!, nombre, archivo?.Length, ct);
            await Comandos.Ejecutar(c, tr,
                "UPDATE public.trabajos SET id_entrega_final=NULL WHERE id_trabajo=@id", ct, ("id", id));
            int version = await Comandos.Valor<int>(c, tr,
                "SELECT numero_version FROM public.entregas WHERE id_entrega=@e", ct, ("e", entrega));
            await avisos.Trabajo(c, tr, id, actor, "VERSION_CORREGIDA_ENVIADA", "Nueva versión recibida para revisión",
                $"El estudiante envió la versión {version}.\nCambios realizados: {motivo}\n\nSe conserva el ejemplar anterior. La nueva versión requiere revisión, verificación, constancia vigente y decisión de Biblioteca antes de publicarse. No hereda permisos de difusión.", true, ct);
            await tr.CommitAsync(CancellationToken.None);
            return StatusCode(201, new { version, ejemplar_id = entrega.ToString(), estado = "En revisión" });
        }
        catch
        {
            if (clave is not null) archivos.QuitarTemporal(clave);
            throw;
        }
    }
}

public sealed class CorreccionEnvio
{
    [FromForm(Name = "ejemplar_id")] public string? EjemplarId { get; init; }
    public string? Cambios { get; init; }
    public IFormFile? Archivo { get; init; }
    [FromForm(Name = "carga_id")] public string? CargaId { get; init; }
    public string? Enlace { get; init; }
}
