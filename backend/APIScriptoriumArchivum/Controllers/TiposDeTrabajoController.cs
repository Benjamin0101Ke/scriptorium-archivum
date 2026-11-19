using APIScriptoriumArchivum.Datos;
using APIScriptoriumArchivum.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace APIScriptoriumArchivum.Controllers
{
    [ApiController]
    [Route("api/tipos-de-trabajo")]
    public class TiposDeTrabajoController : ControllerBase
    {
        private Catalogos catalogos;

        public TiposDeTrabajoController(Catalogos catalogos)
        {
            this.catalogos = catalogos;
        }

        [HttpGet]
        public async Task<ActionResult<List<TipoDeTrabajoDto>>> Listar(CancellationToken cancellationToken)
        {
            List<TipoDeTrabajoDto> opciones = await catalogos.ListarTiposDeTrabajoAsync(cancellationToken);
            return Ok(opciones);
        }
    }
}
