using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TiendaVirtual.Dominio.Servicios.VentaXqm;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.VentaXqm;

namespace TiendaVirtual.Api.Controllers.VentaXqm
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "CLIENTE,VENDEDOR,ADMIN,VERIFICADOR")]
    public class OrdenPagoController : ControllerBase
    {
        private readonly IOrdenPagoServicio _servicio;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public OrdenPagoController(IOrdenPagoServicio servicio, IHttpContextAccessor httpContextAccessor)
        {
            _servicio = servicio;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>Inicia el cobro de una orden (Izipay / modo demo).</summary>
        [HttpPost("iniciar-cobro")]
        public async Task<ActionResult<ResultadoOperacion<RespuestaInicioPagoOrdenDto>>> IniciarCobro(
            [FromBody] IniciarPagoOrdenDto dto)
        {
            var usuarioId = ObtenerUsuarioId();
            if (usuarioId == null) return Unauthorized();
            var r = await _servicio.IniciarPagoAsync(usuarioId.Value, dto);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        /// <summary>Confirma el resultado del pago (webhook Izipay o simulación demo).</summary>
        [HttpPost("confirmar")]
        public async Task<ActionResult<ResultadoOperacion<TransaccionOrdenDto>>> Confirmar(
            [FromBody] ConfirmarPagoOrdenDto dto)
        {
            var r = await _servicio.ConfirmarPagoAsync(dto, ObtenerUsuarioId());
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
