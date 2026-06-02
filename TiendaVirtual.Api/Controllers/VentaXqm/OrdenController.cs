using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TiendaVirtual.Dominio.Servicios.VentaXqm;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.Sistema;
using TiendaVirtual.Intercambio.Dto.VentaXqm;

namespace TiendaVirtual.Api.Controllers.VentaXqm
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "CLIENTE,VENDEDOR")]
    public class OrdenController : ControllerBase
    {
        private readonly IOrdenServicio _servicio;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public OrdenController(IOrdenServicio servicio, IHttpContextAccessor httpContextAccessor)
        {
            _servicio = servicio;
            _httpContextAccessor = httpContextAccessor;
        }

        [HttpPost]
        public async Task<ActionResult<ResultadoOperacion<OrdenDto>>> Crear([FromBody] CrearOrdenDto dto)
        {
            var uid = ObtenerUsuarioId();
            if (uid == null) return Unauthorized();
            var r = await _servicio.CrearAsync(uid.Value, dto);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpGet("mis-ordenes")]
        public async Task<ActionResult<ResultadoOperacion<PaginacionRespuestaDto<OrdenListadoDto>>>> Listar(
            [FromQuery] int pagina = 1, [FromQuery] int tamanioPagina = 10)
        {
            var uid = ObtenerUsuarioId();
            if (uid == null) return Unauthorized();
            var r = await _servicio.ListarMisOrdenesAsync(uid.Value, pagina, tamanioPagina);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpGet("{id:long}")]
        public async Task<ActionResult<ResultadoOperacion<OrdenDto>>> Obtener(long id)
        {
            var uid = ObtenerUsuarioId();
            if (uid == null) return Unauthorized();
            var r = await _servicio.ObtenerMiOrdenAsync(uid.Value, id);
            return r.Exito ? Ok(r) : NotFound(r);
        }

        private int? ObtenerUsuarioId()
        {
            var claim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier);
            return int.TryParse(claim?.Value, out var id) ? id : null;
        }
    }
}
