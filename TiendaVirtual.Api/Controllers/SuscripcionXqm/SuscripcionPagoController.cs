using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TiendaVirtual.Dominio.Servicios.SuscripcionXqm;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.PagoXqm;
using TiendaVirtual.Intercambio.Dto.VendedorXqm;
using TiendaVirtual.Intercambio.Dto.VentaXqm;

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

        [EnableRateLimiting("pagos")]
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

        [EnableRateLimiting("pagos")]
        [HttpPost("procesar")]
        [Authorize(Roles = "VENDEDOR")]
        public async Task<ActionResult<ResultadoOperacion<ResultadoProcesarPagoOrdenDto>>> Procesar(
            [FromBody] ProcesarPagoSuscripcionDto dto)
        {
            var usuarioId = ObtenerUsuarioId();
            if (usuarioId == null) return Unauthorized();

            if (ParecePan(dto.Token))
                return BadRequest(ResultadoOperacion<ResultadoProcesarPagoOrdenDto>.SetError(
                    "No se permiten datos de tarjeta en claro. Use tokenización del SDK."));

            var r = await _servicio.ProcesarPagoAsync(usuarioId.Value, dto);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        /// <summary>Confirmación demo Izipay (solo Development). Bloqueado para Mercado Pago.</summary>
        [HttpPost("confirmar")]
        [Authorize(Roles = "VENDEDOR")]
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

        private Guid? ObtenerUsuarioId()
        {
            if (User.Identity?.IsAuthenticated != true)
                return null;
            var claim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier);
            return Guid.TryParse(claim?.Value, out var id) && id != Guid.Empty ? id : null;
        }

        private static bool ParecePan(string? token)
        {
            var digits = new string((token ?? string.Empty).Where(char.IsDigit).ToArray());
            return digits.Length >= 13 && digits.Length <= 19 &&
                   (token ?? string.Empty).All(c => char.IsDigit(c) || char.IsWhiteSpace(c) || c == '-');
        }
    }
}
