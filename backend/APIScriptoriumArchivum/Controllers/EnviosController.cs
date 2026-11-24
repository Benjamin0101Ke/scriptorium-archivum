using System.Security.Claims;
using APIScriptoriumArchivum.Datos;
using APIScriptoriumArchivum.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace APIScriptoriumArchivum.Controllers;

[ApiController]
[Authorize(Roles = "Lector,Estudiante")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("api/mis-envios")]
public class EnviosController(NpgsqlDataSource baseDeDatos, Fichas fichas, ArchivoPdf archivos, Avisos avisos, CargasArchivos cargas)
    : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken ct)
    {
        await using var conexion = await baseDeDatos.OpenConnectionAsync(ct);
        return Ok(await Comandos.FilasJson(conexion, """
            SELECT jsonb_build_object(
                'id', t.id_trabajo::text,
                'titulo', t.titulo,
                'tipo', tt.nombre,
                'estado', replace(t.estado_publicacion, 'En revisión', 'En revision'),
                'fecha', t.fecha_registro,
                'revision_docente', CASE WHEN rd.id_revision IS NULL THEN null ELSE jsonb_build_object(
                    'resultado', rd.resultado,
                    'observaciones', rd.motivo_decision,
                    'fecha', rd.fecha_cierre,
                    'docente', coalesce(ru.nombre_mostrar, concat_ws(' ', rp.nombres, rp.apellidos)),
                    'version', rd.numero_version) END
            )::text
            FROM public.trabajos t
            JOIN public.tipos_trabajo tt USING (id_tipo_trabajo)
            LEFT JOIN LATERAL (
                SELECT r.*, e.numero_version
                FROM public.entregas e
                JOIN public.revisiones r ON r.id_entrega = e.id_entrega
                WHERE e.id_trabajo = t.id_trabajo
                  AND r.ambito_revision = 'Revisión docente' AND r.fecha_cierre IS NOT NULL
                ORDER BY e.numero_version DESC, r.fecha_cierre DESC, r.id_revision DESC
                LIMIT 1
            ) rd ON true
            LEFT JOIN public.usuarios ru ON ru.id_usuario = rd.id_usuario_revisor
            LEFT JOIN public.personas rp ON rp.id_persona = ru.id_persona
            WHERE t.id_usuario_envio = @actor
            ORDER BY t.id_trabajo DESC
            """, ct, ("actor", Actor())));
    }

    [HttpPost]
    [RequestSizeLimit(CargasArchivos.MaximoBytes + 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = CargasArchivos.MaximoBytes)]
    public async Task<IActionResult> Enviar([FromForm] EnvioLector datos, CancellationToken ct)
    {
        await using var recibido = await cargas.Resolver(datos.Archivo, datos.CargaId, Actor(), ct);
        IFormFile? archivo = recibido.Archivo;
        string? tipo;

        await using (var consultaTipo = await baseDeDatos.OpenConnectionAsync(ct))
        {
            tipo = await Comandos.Valor<string>(
                consultaTipo,
                null,
                "SELECT nombre FROM public.tipos_trabajo WHERE id_tipo_trabajo=@id",
                ct,
                ("id", datos.IdTipoTrabajo));
        }

        if (tipo is null)
            return BadRequest(new { error = "Selecciona un tipo de trabajo válido." });

        bool esSoftware = tipo.Trim().Equals(
            "Proyecto de software",
            StringComparison.OrdinalIgnoreCase);

        string? enlace = datos.Enlace?.Trim();

        if (esSoftware)
        {
            if (archivo is not null)
                return BadRequest(new { error = "El proyecto de software debe registrarse mediante un enlace, no con un archivo." });

            if (string.IsNullOrWhiteSpace(enlace) ||
                enlace.Length > 2048 ||
                !Uri.TryCreate(enlace, UriKind.Absolute, out Uri? url) ||
                url.Scheme != Uri.UriSchemeHttps ||
                (string.IsNullOrWhiteSpace(url.Host) || !string.IsNullOrEmpty(url.UserInfo)))
                return BadRequest(new { error = "Ingresa un enlace HTTPS válido para tu proyecto." });
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(enlace))
                return BadRequest(new { error = "Este tipo de trabajo requiere un PDF, no un enlace." });

            if (archivo is null ||
                archivo.Length < 5 ||
                archivo.Length > CargasArchivos.MaximoBytes ||
                !archivo.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { error = "Selecciona un PDF de hasta 6 GB." });
        }

        string? clave = null;

        if (archivo is not null)
        {
            try
            {
                clave = await archivos.Guardar(archivo, ct);
            }
            catch (InvalidDataException)
            {
                return BadRequest(new { error = "Selecciona un PDF válido." });
            }
        }

        int actor = Actor();
        string nombre = archivo is null
            ? "Repositorio del proyecto"
            : Path.GetFileName(archivo.FileName.Replace('\\', '/'));

        if (nombre.Length > 255) nombre = "ejemplar.pdf";

        try
        {
            await using var conexion = await baseDeDatos.OpenConnectionAsync(ct);
            await using var tr = await conexion.BeginTransactionAsync(ct);

            int persona;
            int carrera;

            await using (var consulta = Comandos.Crear(conexion, tr,
                "SELECT id_persona, id_carrera FROM public.usuarios WHERE id_usuario = @actor AND estado = 'Habilitada' FOR SHARE",
                ("actor", actor)))
            await using (var lector = await consulta.ExecuteReaderAsync(ct))
            {
                if (!await lector.ReadAsync(ct))
                    throw new ErrorApi(403, "La cuenta no está habilitada.");

                persona = lector.GetInt32(0);

                if (lector.IsDBNull(1))
                    throw new ErrorApi(409, "Completa tu carrera en Mi perfil antes de enviar el trabajo.");

                carrera = lector.GetInt32(1);
            }

            if (datos.IdCarrera != 0 && datos.IdCarrera != carrera)
                throw new ErrorApi(409, "La carrera del envío debe coincidir con tu perfil. Recarga la página.");

            var metadata = new MetadatosFicha
            {
                Titulo = datos.Titulo,
                Anio = datos.Anio,
                Resumen = datos.Resumen,
                IdCarrera = carrera,
                IdTipoTrabajo = datos.IdTipoTrabajo,
                AreaIds = datos.Areas,
                AutorIds = [persona]
            };

            int id = await fichas.Guardar(conexion, tr, null, metadata, ct, actor);

            if (esSoftware)
            {
                await Ejemplares.AgregarVersion(
                    conexion, tr, id, actor, "ENLACE", enlace!, nombre, null, ct);
            }
            else
            {
                await Ejemplares.AgregarVersion(
                    conexion, tr, id, actor, "PDF", clave!, nombre, archivo!.Length, ct);
            }

            var filas = await Comandos.FilasJson(conexion, tr, """
                SELECT jsonb_build_object('id', t.id_trabajo::text, 'titulo', t.titulo, 'tipo', tt.nombre,
                    'estado', 'En revision', 'fecha', t.fecha_registro)::text
                FROM public.trabajos t JOIN public.tipos_trabajo tt USING (id_tipo_trabajo)
                WHERE t.id_trabajo = @id
                """, ct, ("id", id));

            await avisos.Trabajo(
                conexion,
                tr,
                id,
                actor,
                "TRABAJO_ENVIADO",
                "Trabajo recibido para revisión",
                esSoftware
                    ? "Se registró el trabajo y el enlace de su proyecto de software. Biblioteca revisará los requisitos antes de decidir su publicación."
                    : "Se registró el trabajo y su PDF. Biblioteca revisará los requisitos antes de decidir su publicación.",
                true,
                ct);

            await tr.CommitAsync(CancellationToken.None);
            return StatusCode(201, filas[0]);
        }
        catch
        {
            if (clave is not null)
                archivos.QuitarTemporal(clave);

            throw;
        }
    }

    private int Actor() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}

public sealed class EnvioLector
{
    public string? Titulo { get; init; }
    public int Anio { get; init; }
    public string? Resumen { get; init; }

    [FromForm(Name = "id_carrera")]
    public int IdCarrera { get; init; }

    [FromForm(Name = "id_tipo_trabajo")]
    public int IdTipoTrabajo { get; init; }

    [FromForm(Name = "area_ids")]
    public int[] Areas { get; init; } = [];

    public IFormFile? Archivo { get; init; }
    [FromForm(Name = "carga_id")]
    public string? CargaId { get; init; }

    [FromForm(Name = "enlace")]
    public string? Enlace { get; init; }
}
