using APIScriptoriumArchivum.Datos;
using APIScriptoriumArchivum.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace APIScriptoriumArchivum.Controllers
{
    // Expone el catalogo de temas que se utiliza para clasificar los trabajos
    [ApiController]
    [Route("api/areas-tematicas")]
    public class AreasTematicasController : ControllerBase
    {
        // Servicio que obtiene los datos de los catalogos
        private Catalogos catalogos;
        // Recibe el servicio que consulta los temas del catalogo
        public AreasTematicasController(Catalogos catalogos)
        {
            this.catalogos = catalogos;
        }

        // Devuelve los identificadores y nombres de las areas tematicas disponibles
        [HttpGet]
        public async Task<ActionResult<List<AreaTematicaDto>>> Listar(CancellationToken cancellationToken)
        {
            List<AreaTematicaDto> opciones = await catalogos.ListarAreasTematicasAsync(cancellationToken);
            return Ok(opciones);
        }
    }
}
