using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TiendaVirtual.Api.Seguridad;
using TiendaVirtual.Dominio.Servicios.VentaXqm;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.Sistema;
using TiendaVirtual.Intercambio.Dto.VentaXqm;

namespace TiendaVirtual.Api.Controllers.VentaXqm
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "CLIENTE,VENDEDOR,ADMIN,VERIFICADOR")]
    public class OrdenController : ControllerBase
    {
        private readonly IOrdenServicio _servicio;

        public OrdenController(IOrdenServicio servicio)
        {
            _servicio = servicio;
        }

        [HttpPost]
        public async Task<ActionResult<ResultadoOperacion<OrdenDto>>> Crear([FromBody] CrearOrdenDto dto)
        {
            var uid = User.ObtenerUsuarioId();
            if (uid == null) return Unauthorized();
            var r = await _servicio.CrearAsync(uid.Value, dto);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpGet("mis-ordenes")]
        public async Task<ActionResult<ResultadoOperacion<PaginacionRespuestaDto<OrdenListadoDto>>>> Listar(
            [FromQuery] int pagina = 1, [FromQuery] int tamanioPagina = 10)
        {
            var uid = User.ObtenerUsuarioId();
            if (uid == null) return Unauthorized();
            var r = await _servicio.ListarMisOrdenesAsync(uid.Value, pagina, tamanioPagina);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        /// <summary>
        /// Detalle de orden propia. El servicio exige ClienteId == usuario del token (anti-IDOR).
        /// </summary>
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ResultadoOperacion<OrdenDto>>> Obtener(Guid id)
        {
            var uid = User.ObtenerUsuarioId();
            if (uid == null) return Unauthorized();
            var r = await _servicio.ObtenerMiOrdenAsync(uid.Value, id);
            return r.Exito ? Ok(r) : NotFound(r);
        }
    }
}
