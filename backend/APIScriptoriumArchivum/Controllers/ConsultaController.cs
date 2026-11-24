using System.Security.Claims;
using APIScriptoriumArchivum.Datos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APIScriptoriumArchivum.Controllers
{
    [ApiController]
    [Route("api")]
    public class ConsultaController : ControllerBase
    {
        private ConsultaPublica consulta;

        public ConsultaController(ConsultaPublica consulta)
        {
            this.consulta = consulta;
        }

        [HttpGet("trabajos")]
        public async Task<IActionResult> Listar([FromQuery] string? q, [FromQuery] string? autor,
            [FromQuery] string? anio, [FromQuery] string? carrera, [FromQuery] string? area,
            [FromQuery] string? tipo, [FromQuery] string? orden, [FromQuery] int? departamento,
            [FromQuery] int? area_academica, CancellationToken ct)
        {
            Response.Headers.CacheControl = "no-store";
            int? anioFiltro = null;
            if (!string.IsNullOrWhiteSpace(anio))
            {
                if (!int.TryParse(anio, out int valor) || valor is < 1000 or > 9999)
                {
                    return BadRequest(new { error = "El año de búsqueda debe tener cuatro dígitos." });
                }
                anioFiltro = valor;
            }
            if (q?.Length > 500 || (!string.IsNullOrEmpty(orden) && orden is not ("coincidencia" or "anio" or "titulo" or "valoracion" or "estrellas")))
            {
                return BadRequest(new { error = "Revisa el texto y el orden de búsqueda." });
            }
            return Ok(await consulta.Listar(q, autor, anioFiltro, carrera, area, tipo, orden, null, ct, departamento ?? area_academica));
        }

        [HttpGet("trabajos/{id:int}")]
        public async Task<IActionResult> Detalle(int id, CancellationToken ct)
        {
            Response.Headers.CacheControl = "no-store";
            return Ok((await consulta.Buscar(id,ct)).Datos);
        }

        [HttpGet("trabajos/{id:int}/pdf")]
        public async Task<IActionResult> Pdf(int id, [FromQuery] string? operacion, CancellationToken ct)
        {
            Response.Headers.CacheControl = "no-store";
            if (operacion is not ("lectura" or "descarga"))
            {
                return BadRequest(new { error = "Indica lectura o descarga." });
            }
            ConsultaPublica.FichaPublica ficha = await consulta.Buscar(id,ct);
            bool permitida = operacion == "lectura" ? ficha.Lectura : ficha.Descarga;
            if (!permitida || ficha.RutaPdf is null)
            {
                return StatusCode(403, new { error = "La operación de PDF no está autorizada." });
            }

            if (operacion == "descarga")
            {
                return PhysicalFile(ficha.RutaPdf, "application/pdf", ficha.NombrePdf ?? "ejemplar.pdf", enableRangeProcessing:true);
            }
            Response.Headers.ContentDisposition = "inline";
            return PhysicalFile(ficha.RutaPdf, "application/pdf", enableRangeProcessing:true);
        }

        [Authorize]
        [HttpGet("favoritos")]
        public async Task<IActionResult> Favoritos(CancellationToken ct)
        {
            Response.Headers.CacheControl = "no-store";
            return Ok(await consulta.Listar(null,null,null,null,null,null,"titulo",Actor(),ct));
        }

        [Authorize]
        [HttpPut("favoritos/{id:int}")]
        public async Task<IActionResult> Agregar(int id, CancellationToken ct)
        {
            await consulta.Favorito(id,Actor(),true,ct);
            return Ok(new { ok = true });
        }

        [Authorize]
        [HttpDelete("favoritos/{id:int}")]
        public async Task<IActionResult> Quitar(int id, CancellationToken ct)
        {
            await consulta.Favorito(id,Actor(),false,ct);
            return Ok(new { ok = true });
        }

        private int Actor()
        {
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        }
    }
}
