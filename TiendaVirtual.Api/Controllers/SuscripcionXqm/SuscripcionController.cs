using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TiendaVirtual.Dominio.Servicios.SuscripcionXqm;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.Sistema;
using TiendaVirtual.Intercambio.Dto.VendedorXqm;

namespace TiendaVirtual.Api.Controllers.SuscripcionXqm
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SuscripcionController : ControllerBase
    {
        private readonly ISuscripcionServicio _servicio;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SuscripcionController(ISuscripcionServicio servicio, IHttpContextAccessor httpContextAccessor)
        {
            _servicio = servicio;
            _httpContextAccessor = httpContextAccessor;
        }

        [HttpGet("mi-suscripcion")]
        [Authorize(Roles = "VENDEDOR")]
        public async Task<ActionResult<ResultadoOperacion<SuscripcionDto?>>> MiSuscripcion()
        {
            var usuarioId = ObtenerUsuarioId();
            if (usuarioId == null) return Unauthorized();
            var r = await _servicio.ObtenerMiSuscripcionAsync(usuarioId.Value);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpGet("elegibilidad")]
        [Authorize(Roles = "VENDEDOR")]
        public async Task<ActionResult<ResultadoOperacion<SuscripcionElegibilidadDto>>> Elegibilidad()
        {
            var usuarioId = ObtenerUsuarioId();
            if (usuarioId == null) return Unauthorized();
            var r = await _servicio.ObtenerElegibilidadAsync(usuarioId.Value);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpPost("iniciar")]
        [Authorize(Roles = "VENDEDOR")]
        public async Task<ActionResult<ResultadoOperacion<SuscripcionDto>>> Iniciar([FromBody] IniciarSuscripcionDto dto)
        {
            var usuarioId = ObtenerUsuarioId();
            if (usuarioId == null) return Unauthorized();
            var r = await _servicio.IniciarAsync(usuarioId.Value, dto);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpPost("reactivar")]
        [Authorize(Roles = "VENDEDOR")]
        public async Task<ActionResult<ResultadoOperacion<SuscripcionDto>>> ReactivarPlan(
            [FromBody] IniciarSuscripcionDto dto)
        {
            var usuarioId = ObtenerUsuarioId();
            if (usuarioId == null) return Unauthorized();
            var r = await _servicio.ReactivarPlanAsync(usuarioId.Value, dto);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpPost("cambiar-plan")]
        [Authorize(Roles = "VENDEDOR")]
        public async Task<ActionResult<ResultadoOperacion<SuscripcionDto>>> Cambiar([FromBody] CambiarPlanDto dto)
        {
            var usuarioId = ObtenerUsuarioId();
            if (usuarioId == null) return Unauthorized();
            var r = await _servicio.CambiarPlanAsync(usuarioId.Value, dto);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpPost("cancelar")]
        [Authorize(Roles = "VENDEDOR")]
        public async Task<ActionResult<ResultadoOperacion<bool>>> Cancelar()
        {
            var usuarioId = ObtenerUsuarioId();
            if (usuarioId == null) return Unauthorized();
            var r = await _servicio.CancelarAsync(usuarioId.Value);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpGet("admin")]
        [Authorize(Roles = "ADMIN")]
        public async Task<ActionResult<ResultadoOperacion<PaginacionRespuestaDto<SuscripcionDto>>>> ListarAdmin(
            [FromQuery] int pagina = 1, [FromQuery] int tamanioPagina = 20)
        {
            var r = await _servicio.ListarAdminAsync(pagina, tamanioPagina);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpPost("admin/{id:int}/suspender")]
        [Authorize(Roles = "ADMIN")]
        public async Task<ActionResult<ResultadoOperacion<bool>>> Suspender(int id)
        {
            var r = await _servicio.SuspenderAsync(id);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpPost("admin/{id:int}/reactivar")]
        [Authorize(Roles = "ADMIN")]
        public async Task<ActionResult<ResultadoOperacion<bool>>> ReactivarSuspendida(int id)
        {
            var r = await _servicio.ReactivarAsync(id);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpPost("admin/procesar-vencimientos")]
        [Authorize(Roles = "ADMIN")]
        public async Task<ActionResult<ResultadoOperacion<int>>> ProcesarVencimientos()
        {
            var r = await _servicio.ProcesarVencimientosAsync();
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        private int? ObtenerUsuarioId()
        {
            var claim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier);
            return int.TryParse(claim?.Value, out var id) ? id : null;
        }
    }
}
