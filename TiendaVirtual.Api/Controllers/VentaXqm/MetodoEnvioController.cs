using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TiendaVirtual.Dominio.Servicios.VentaXqm;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.VentaXqm;

namespace TiendaVirtual.Api.Controllers.VentaXqm
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class MetodoEnvioController : ControllerBase
    {
        private readonly IMetodoEnvioServicio _servicio;

        public MetodoEnvioController(IMetodoEnvioServicio servicio)
        {
            _servicio = servicio;
        }

        [HttpGet]
        public async Task<ActionResult<ResultadoOperacion<List<MetodoEnvioDto>>>> Listar()
        {
            var r = await _servicio.ListarActivosAsync();
            return r.Exito ? Ok(r) : BadRequest(r);
        }
    }
}
