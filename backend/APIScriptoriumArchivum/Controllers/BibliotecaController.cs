using System.Security.Claims;
using System.Text.Json;
using APIScriptoriumArchivum.Datos;
using APIScriptoriumArchivum.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APIScriptoriumArchivum.Controllers
{
    [ApiController]
    [Authorize(Roles = "Biblioteca")]
    [Route("api/admin")]
    public class BibliotecaController : ControllerBase
    {
        private readonly Fichas fichas;
        private readonly Ejemplares ejemplares;
        private readonly ArchivoPdf archivos;
        private readonly Complementos complementos;

        public BibliotecaController(Fichas fichas, Ejemplares ejemplares, ArchivoPdf archivos, Complementos complementos)
        {
            this.fichas = fichas;
            this.ejemplares = ejemplares;
            this.archivos = archivos;
            this.complementos = complementos;
        }

        [HttpGet("personas")]
        public async Task<IActionResult> Personas([FromQuery] string? q, CancellationToken ct)
        {
            return Ok(await fichas.Personas(q, ct));
        }

        [HttpPost("personas")]
        public async Task<IActionResult> CrearPersona(PersonaDatos datos, CancellationToken ct)
        {
            int id = await fichas.CrearPersona(datos, ct);
            return StatusCode(201, new { id });
        }

        [HttpGet("trabajos")]
        public async Task<IActionResult> Listar(CancellationToken ct)
        {
            return Ok(await fichas.Listar(ct));
        }

        [HttpGet("trabajos/{id:int}")]
        public async Task<IActionResult> Detalle(int id, CancellationToken ct)
        {
            return Ok(await ejemplares.Detalle(id, ct));
        }

        [HttpPost("trabajos")]
        public async Task<IActionResult> Crear(MetadatosFicha datos, CancellationToken ct)
        {
            int id = await fichas.Guardar(null, datos, ct);
            return CreatedAtAction(nameof(Detalle), new { id }, new { id = id.ToString() });
        }

        [HttpPut("trabajos/{id:int}")]
        public async Task<IActionResult> Editar(int id, MetadatosFicha datos, CancellationToken ct)
        {
            await fichas.Guardar(id, datos, ct, Actor());
            return Ok(new { id = id.ToString() });
        }

        [HttpPost("trabajos/{id:int}/referencia")]
        public async Task<IActionResult> Referencia(int id, ReferenciaEjemplar datos, CancellationToken ct)
        {
            string? referencia = Fichas.TextoOpcional(datos.Referencia);
            if (referencia is null || referencia.Length > 2000)
            {
                return BadRequest(new { error = "Indica la referencia institucional del ejemplar." });
            }
            int ejemplar = await ejemplares.Registrar(id, Actor(), "REF_UES", referencia, null, null, ct);
            return Ok(new { ejemplar_id = ejemplar.ToString() });
        }

        [HttpPost("trabajos/{id:int}/pdf")]
        [RequestSizeLimit(26L * 1024 * 1024)]
        public async Task<IActionResult> SubirPdf(int id, [FromForm] IFormFile? archivo, CancellationToken ct)
        {
            if (archivo is null || archivo.Length < 5 || archivo.Length > 25L * 1024 * 1024 ||
                !archivo.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { error = "Selecciona un PDF de hasta 25 MB." });
            }

            string clave;
            try
            {
                clave = await archivos.Guardar(archivo, ct);
            }
            catch (InvalidDataException)
            {
                return BadRequest(new { error = "El archivo debe ser un PDF." });
            }

            string nombre = Path.GetFileName(archivo.FileName.Replace('\\', '/'));
            if (nombre.Length > 255)
            {
                nombre = "ejemplar.pdf";
            }

            int ejemplar;
            try
            {
                ejemplar = await ejemplares.Registrar(id, Actor(), "PDF", clave, nombre, archivo.Length, ct);
            }
            catch
            {
                archivos.QuitarTemporal(clave);
                throw;
            }
            return Ok(new { ejemplar_id = ejemplar.ToString() });
        }

        [HttpGet("trabajos/{id:int}/pdf")]
        public async Task<IActionResult> VerPdf(int id, [FromQuery] int? ejemplar, CancellationToken ct)
        {
            Response.Headers.CacheControl = "no-store";
            if (ejemplar is <= 0)
            {
                return BadRequest(new { error = "Indica un identificador de ejemplar válido." });
            }

            string ruta = await ejemplares.PdfBiblioteca(id, ejemplar, ct);
            return PhysicalFile(ruta, "application/pdf", enableRangeProcessing: true);
        }

        [HttpPut("trabajos/{id:int}/controles")]
        public async Task<IActionResult> GuardarControles(int id, ControlesFicha datos, CancellationToken ct)
        {
            await ejemplares.GuardarControles(id, Actor(), datos, ct);
            return Ok(new { ok = true });
        }

        [HttpPost("trabajos/{id:int}/publicar")]
        public async Task<IActionResult> Publicar(int id, CancellationToken ct)
        {
            await ejemplares.Publicar(id, Actor(), ct);
            return Ok(new { ok = true });
        }

        [HttpPost("trabajos/{id:int}/retirar")]
        public async Task<IActionResult> Retirar(int id, MotivoRetiro datos, CancellationToken ct)
        {
            await ejemplares.Retirar(id, Actor(), datos.Motivo, ct);
            return Ok(new { ok = true });
        }

        [HttpGet("trabajos/{id:int}/enlaces")]
        public async Task<IActionResult> ListarEnlaces(int id, CancellationToken ct)
        {
            return Ok(await complementos.ListarEnlaces(id, ct));
        }

        [HttpPost("trabajos/{id:int}/enlaces")]
        public async Task<IActionResult> CrearEnlace(int id, Complementos.DatosEnlace datos, CancellationToken ct)
        {
            int enlace = await complementos.CrearEnlace(id, datos, ct, Actor());
            return StatusCode(201, new { id = enlace.ToString() });
        }

        [HttpPatch("enlaces/{id:int}")]
        public async Task<IActionResult> Habilitar(int id, Complementos.EstadoEnlace datos, CancellationToken ct)
        {
            if (!datos.Habilitado.HasValue)
            {
                return BadRequest(new { error = "Indica si el enlace quedará habilitado." });
            }
            await complementos.Habilitar(id, datos.Habilitado.Value, ct, Actor());
            return Ok(new { ok = true });
        }

        [HttpDelete("trabajos/{id:int}/enlaces/{enlace:int}")]
        public async Task<IActionResult> EliminarEnlace(int id, int enlace, CancellationToken ct)
        {
            await complementos.EliminarEnlace(id, enlace, ct, Actor());
            return Ok(new { ok = true });
        }

        [HttpGet("bitacora")]
        public async Task<IActionResult> Bitacora(CancellationToken ct)
        {
            return Ok(await complementos.Bitacora(ct));
        }

        [HttpGet("guia")]
        public async Task<IActionResult> Guia(CancellationToken ct)
        {
            JsonElement? guia = await complementos.Guia(ct);
            if (!guia.HasValue)
            {
                return Ok(new { disponible = false, mensaje = "No se ha registrado una guía vigente." });
            }
            return Ok(guia.Value);
        }

        [HttpGet("reporte")]
        public async Task<IActionResult> Reporte(CancellationToken ct)
        {
            return Ok(await complementos.Reporte(ct));
        }

        private int Actor()
        {
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        }
    }
}
