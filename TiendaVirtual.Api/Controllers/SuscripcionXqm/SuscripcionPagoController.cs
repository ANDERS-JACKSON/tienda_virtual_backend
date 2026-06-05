using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TiendaVirtual.Dominio.Servicios.SuscripcionXqm;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.PagoXqm;
using TiendaVirtual.Intercambio.Dto.VendedorXqm;

namespace TiendaVirtual.Api.Controllers.SuscripcionXqm
{
    [ApiController]
    [Route("api/[controller]")]
    public class SuscripcionPagoController : ControllerBase
    {
        private readonly ISuscripcionPagoServicio _servicio;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SuscripcionPagoController(ISuscripcionPagoServicio servicio, IHttpContextAccessor httpContextAccessor)
        {
            _servicio = servicio;
            _httpContextAccessor = httpContextAccessor;
        }

        [HttpPost("iniciar-cobro")]
        [Authorize(Roles = "VENDEDOR")]
        public async Task<ActionResult<ResultadoOperacion<RespuestaInicioPagoDto>>> IniciarCobro(
            [FromBody] IniciarPagoSuscripcionDto dto)
        {
            var usuarioId = ObtenerUsuarioId();
            if (usuarioId == null) return Unauthorized();
            var r = await _servicio.IniciarPagoAsync(usuarioId.Value, dto);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpPost("confirmar")]
        [AllowAnonymous]
        public async Task<ActionResult<ResultadoOperacion<TransaccionDto>>> Confirmar(
            [FromBody] ConfirmarPagoSuscripcionDto dto)
        {
            var r = await _servicio.ConfirmarPagoAsync(dto, ObtenerUsuarioId());
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpGet("mis-transacciones")]
        [Authorize(Roles = "VENDEDOR")]
        public async Task<ActionResult<ResultadoOperacion<List<TransaccionDto>>>> MisTransacciones()
        {
            var usuarioId = ObtenerUsuarioId();
            if (usuarioId == null) return Unauthorized();
            var r = await _servicio.ListarMisTransaccionesAsync(usuarioId.Value);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        private int? ObtenerUsuarioId()
        {
            if (User.Identity?.IsAuthenticated != true)
                return null;
            var claim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier);
            return int.TryParse(claim?.Value, out var id) ? id : null;
        }
    }
}
