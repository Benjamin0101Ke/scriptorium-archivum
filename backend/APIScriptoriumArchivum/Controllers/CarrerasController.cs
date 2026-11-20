using APIScriptoriumArchivum.Datos;
using APIScriptoriumArchivum.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace APIScriptoriumArchivum.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CarrerasController : ControllerBase
    {
        private Catalogos catalogos;

        public CarrerasController(Catalogos catalogos)
        {
            this.catalogos = catalogos;
        }

        [HttpGet]
        public async Task<ActionResult<List<CarreraDto>>> Listar(CancellationToken cancellationToken)
        {
            List<CarreraDto> opciones = await catalogos.ListarCarrerasAsync(cancellationToken);
            return Ok(opciones);
        }
    }
}
