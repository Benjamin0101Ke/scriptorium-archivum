using System.Security.Claims;
using System.Text.Json;
using APIScriptoriumArchivum.Datos;
using APIScriptoriumArchivum.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APIScriptoriumArchivum.Controllers
{
    // Permite a Biblioteca registrar fichas, revisar ejemplares y decidir su publicacion
    [ApiController]
    [Authorize(Roles = "Biblioteca")]
    [Route("api/admin")]
    public class BibliotecaController : ControllerBase
    {
        // Gestion de metadatos, autores y areas de los trabajos
        private Fichas fichas;
        // Control de versiones, verificaciones y publicacion de ejemplares
        private Ejemplares ejemplares;
        // Validacion y almacenamiento de archivos PDF
        private ArchivoPdf archivos;
        // Gestion de enlaces y consultas complementarias de Biblioteca
        private Complementos complementos;
        // Gestion de cargas temporales de archivos por partes
        private CargasArchivos cargas;
        // Recibe los servicios de fichas, ejemplares, archivos y material complementario
        public BibliotecaController(Fichas fichas, Ejemplares ejemplares, ArchivoPdf archivos, Complementos complementos, CargasArchivos cargas)
        {
            this.fichas = fichas;
            this.ejemplares = ejemplares;
            this.archivos = archivos;
            this.complementos = complementos;
            this.cargas = cargas;
        }

        // Busca las personas registradas que pueden asociarse al trabajo
        [HttpGet("personas")]
        public async Task<IActionResult> Personas([FromQuery] string? q, CancellationToken ct)
        {
            return Ok(await fichas.Personas(q, ct));
        }

        // Registra una persona y devuelve su identificador
        [HttpPost("personas")]
        public async Task<IActionResult> CrearPersona(PersonaDatos datos, CancellationToken ct)
        {
            int id = await fichas.CrearPersona(datos, ct);
            return StatusCode(201, new { id });
        }

        // Devuelve las fichas para la gestion de Biblioteca
        [HttpGet("trabajos")]
        public async Task<IActionResult> Listar(CancellationToken ct)
        {
            return Ok(await fichas.Listar(ct));
        }

        // Consulta los datos y controles del trabajo seleccionado
        [HttpGet("trabajos/{id:int}")]
        public async Task<IActionResult> Detalle(int id, CancellationToken ct)
        {
            return Ok(await ejemplares.Detalle(id, ct));
        }

        // Registra una ficha academica con sus autores y areas tematicas
        [HttpPost("trabajos")]
        public async Task<IActionResult> Crear(MetadatosFicha datos, CancellationToken ct)
        {
            int id = await fichas.Guardar(null, datos, ct);
            return CreatedAtAction(nameof(Detalle), new { id }, new { id = id.ToString() });
        }

        // Actualiza los metadatos de una ficha existente
        [HttpPut("trabajos/{id:int}")]
        public async Task<IActionResult> Editar(int id, MetadatosFicha datos, CancellationToken ct)
        {
            await fichas.Guardar(id, datos, ct, Actor());
            return Ok(new { id = id.ToString() });
        }

        // Asocia una referencia UES como nueva version del ejemplar
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

        // Recibe el PDF enviado por Angular y lo asocia como una nueva entrega del trabajo
        [HttpPost("trabajos/{id:int}/pdf")]
        [RequestSizeLimit(CargasArchivos.MaximoBytes + 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = CargasArchivos.MaximoBytes)]
        public async Task<IActionResult> SubirPdf(int id, [FromForm] IFormFile? archivo, [FromForm(Name = "carga_id")] string? cargaId, CancellationToken ct)
        {
            // Obtiene el archivo de la peticion o de una carga por partes ya preparada
            await using var recibido = await cargas.Resolver(archivo, cargaId, Actor(), ct);
            archivo = recibido.Archivo;
            if (archivo is null || archivo.Length < 5 || !archivo.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { error = "Selecciona un PDF válido." });
            }

            string clave;
            try
            {
                // ArchivoPdf escribe el contenido en disco y devuelve la clave del archivo
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
                // Registra el ejemplar y la referencia al archivo en la base de datos
                ejemplar = await ejemplares.Registrar(id, Actor(), "PDF", clave, nombre, archivo.Length, ct);
            }
            catch
            {
                // Evita dejar un archivo sin referencia si no se pudo registrar el ejemplar
                archivos.QuitarTemporal(clave);
                throw;
            }

            return Ok(new { ejemplar_id = ejemplar.ToString() });
        }

        // Permite revisar el PDF del ejemplar solicitado desde Biblioteca
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

        // Guarda verificaciones, constancia, permisos y decision del ejemplar indicado
        [HttpPut("trabajos/{id:int}/controles")]
        public async Task<IActionResult> GuardarControles(int id, ControlesFicha datos, CancellationToken ct)
        {
            await ejemplares.GuardarControles(id, Actor(), datos, ct);
            return Ok(new { ok = true });
        }

        // Solicita la publicacion solo cuando el ejemplar cumple los controles requeridos
        [HttpPost("trabajos/{id:int}/publicar")]
        public async Task<IActionResult> Publicar(int id, CancellationToken ct)
        {
            await ejemplares.Publicar(id, Actor(), ct);
            return Ok(new { ok = true });
        }

        // Retira la ficha publica y conserva el motivo y responsable de la decision
        [HttpPost("trabajos/{id:int}/retirar")]
        public async Task<IActionResult> Retirar(int id, MotivoRetiro datos, CancellationToken ct)
        {
            await ejemplares.Retirar(id, Actor(), datos.Motivo, ct);
            return Ok(new { ok = true });
        }

        // Consulta los enlaces complementarios asociados al trabajo
        [HttpGet("trabajos/{id:int}/enlaces")]
        public async Task<IActionResult> ListarEnlaces(int id, CancellationToken ct)
        {
            return Ok(await complementos.ListarEnlaces(id, ct));
        }

        // Agrega un enlace complementario con su tipo y descripcion
        [HttpPost("trabajos/{id:int}/enlaces")]
        public async Task<IActionResult> CrearEnlace(int id, Complementos.DatosEnlace datos, CancellationToken ct)
        {
            int enlace = await complementos.CrearEnlace(id, datos, ct, Actor());
            return StatusCode(201, new { id = enlace.ToString() });
        }

        // Activa o desactiva la difusion del enlace seleccionado
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

        // Elimina la asociacion del enlace complementario con el trabajo
        [HttpDelete("trabajos/{id:int}/enlaces/{enlace:int}")]
        public async Task<IActionResult> EliminarEnlace(int id, int enlace, CancellationToken ct)
        {
            await complementos.EliminarEnlace(id, enlace, ct, Actor());
            return Ok(new { ok = true });
        }

        // Consulta el registro de acciones realizadas sobre el repositorio
        [HttpGet("bitacora")]
        public async Task<IActionResult> Bitacora(CancellationToken ct)
        {
            return Ok(await complementos.Bitacora(ct));
        }

        // Devuelve el procedimiento de deposito vigente cuando hay uno registrado
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

        // Obtiene el resumen disponible para la gestion de Biblioteca
        [HttpGet("reporte")]
        public async Task<IActionResult> Reporte(CancellationToken ct)
        {
            return Ok(await complementos.Reporte(ct));
        }

        // Identifica la cuenta conectada para registrar quien realiza la accion
        private int Actor()
        {
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        }
    }
}
