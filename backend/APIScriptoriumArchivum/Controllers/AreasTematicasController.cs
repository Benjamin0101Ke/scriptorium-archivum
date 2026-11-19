using APIScriptoriumArchivum.Datos;
using APIScriptoriumArchivum.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace APIScriptoriumArchivum.Controllers
{
    [ApiController]
    [Route("api/areas-tematicas")]
    public class AreasTematicasController : ControllerBase
    {
        private Catalogos catalogos;

        public AreasTematicasController(Catalogos catalogos)
        {
            this.catalogos = catalogos;
        }

        [HttpGet]
        public async Task<ActionResult<List<AreaTematicaDto>>> Listar(CancellationToken cancellationToken)
        {
            List<AreaTematicaDto> opciones = await catalogos.ListarAreasTematicasAsync(cancellationToken);
            return Ok(opciones);
        }
    }
}
