using APIScriptoriumArchivum.Datos;
using APIScriptoriumArchivum.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace APIScriptoriumArchivum.Controllers
{
    // Entrega las carreras disponibles para los formularios y filtros
    [ApiController]
    [Route("api/[controller]")]
    public class CarrerasController : ControllerBase
    {
        // Servicio que obtiene los datos de los catalogos
        private Catalogos catalogos;
        // Recibe el servicio de consulta de carreras
        public CarrerasController(Catalogos catalogos)
        {
            this.catalogos = catalogos;
        }

        // Obtiene las carreras disponibles y devuelve sus datos basicos
        [HttpGet]
        public async Task<ActionResult<List<CarreraDto>>> Listar(CancellationToken cancellationToken)
        {
            List<CarreraDto> opciones = await catalogos.ListarCarrerasAsync(cancellationToken);
            return Ok(opciones);
        }
    }
}
