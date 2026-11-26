using APIScriptoriumArchivum.Datos;
using APIScriptoriumArchivum.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace APIScriptoriumArchivum.Controllers
{
    // Publica el catalogo de tipos de trabajo para su seleccion
    [ApiController]
    [Route("api/tipos-de-trabajo")]
    public class TiposDeTrabajoController : ControllerBase
    {
        // Servicio que obtiene los datos de los catalogos
        private Catalogos catalogos;
        // Recibe la consulta de tipos de trabajo
        public TiposDeTrabajoController(Catalogos catalogos)
        {
            this.catalogos = catalogos;
        }

        // Devuelve los tipos de trabajo disponibles para seleccionarlos en la interfaz
        [HttpGet]
        public async Task<ActionResult<List<TipoDeTrabajoDto>>> Listar(CancellationToken cancellationToken)
        {
            List<TipoDeTrabajoDto> opciones = await catalogos.ListarTiposDeTrabajoAsync(cancellationToken);
            return Ok(opciones);
        }
    }
}
