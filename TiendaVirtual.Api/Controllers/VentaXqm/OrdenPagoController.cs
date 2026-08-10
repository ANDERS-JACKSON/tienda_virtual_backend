using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
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

        /// <summary>
        /// PublicKey y monto estimado del carrito para abrir el modal de pago
        /// sin crear todavía la orden.
        /// </summary>
        [HttpGet("configuracion-checkout")]
        public async Task<ActionResult<ResultadoOperacion<ConfiguracionCheckoutPagoDto>>> ConfiguracionCheckout()
        {
            var usuarioId = ObtenerUsuarioId();
            if (usuarioId == null) return Unauthorized();
            var r = await _servicio.ObtenerConfiguracionCheckoutAsync(usuarioId.Value);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        /// <summary>
        /// Cobro atómico desde el carrito: la orden solo queda firme si el pago
        /// es aprobado o queda pendiente de confirmación. Si falla, se anula la
        /// reserva y el carrito se conserva.
        /// </summary>
        [EnableRateLimiting("pagos")]
        [HttpPost("cobrar-carrito")]
        public async Task<ActionResult<ResultadoOperacion<ResultadoCobrarCarritoDto>>> CobrarCarrito(
            [FromBody] CobrarCarritoDto dto)
        {
            var usuarioId = ObtenerUsuarioId();
            if (usuarioId == null) return Unauthorized();

            // ConfirmarDemo: no usa Token; el check PAN se omite solo en ese bypass de Development.
            if (!dto.ConfirmarDemo && PareceContenerPanToken(dto.Token))
                return BadRequest(ResultadoOperacion<ResultadoCobrarCarritoDto>.SetError(
                    "No se permiten datos de tarjeta en claro. Use tokenización del SDK."));

            var r = await _servicio.CobrarCarritoAsync(usuarioId.Value, dto);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        /// <summary>Inicia el cobro de una orden (crea/reusa Transaccion + datos del proveedor activo).</summary>
        [EnableRateLimiting("pagos")]
        [HttpPost("iniciar-cobro")]
        public async Task<ActionResult<ResultadoOperacion<RespuestaInicioPagoOrdenDto>>> IniciarCobro(
            [FromBody] IniciarPagoOrdenDto dto)
        {
            var usuarioId = ObtenerUsuarioId();
            if (usuarioId == null) return Unauthorized();
            var r = await _servicio.IniciarPagoAsync(usuarioId.Value, dto);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        /// <summary>
        /// Procesa el pago con token de Checkout API (tarjeta / Yape).
        /// No acepta datos de tarjeta en claro.
        /// </summary>
        [EnableRateLimiting("pagos")]
        [HttpPost("procesar")]
        public async Task<ActionResult<ResultadoOperacion<ResultadoProcesarPagoOrdenDto>>> Procesar(
            [FromBody] ProcesarPagoOrdenDto dto)
        {
            var usuarioId = ObtenerUsuarioId();
            if (usuarioId == null) return Unauthorized();

            if (PareceContenerPan(dto))
                return BadRequest(ResultadoOperacion<ResultadoProcesarPagoOrdenDto>.SetError(
                    "No se permiten datos de tarjeta en claro. Use tokenización del SDK."));

            var r = await _servicio.ProcesarPagoAsync(usuarioId.Value, dto);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        /// <summary>
        /// Verifica el estado de un pedido pendiente de pago. Si Mercado Pago
        /// ya tiene un cobro en curso, solo consulta (nunca crea un segundo cobro).
        /// </summary>
        [HttpPost("verificar-pago")]
        public async Task<ActionResult<ResultadoOperacion<ResultadoVerificarPagoOrdenDto>>> VerificarPago(
            [FromBody] VerificarPagoOrdenDto dto)
        {
            var usuarioId = ObtenerUsuarioId();
            if (usuarioId == null) return Unauthorized();
            var r = await _servicio.VerificarPagoOrdenAsync(usuarioId.Value, dto);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        /// <summary>
        /// Confirmación demo Izipay (solo Development). Bloqueado para Mercado Pago.
        /// </summary>
        [HttpPost("confirmar")]
        public async Task<ActionResult<ResultadoOperacion<TransaccionOrdenDto>>> Confirmar(
            [FromBody] ConfirmarPagoOrdenDto dto)
        {
            var r = await _servicio.ConfirmarPagoAsync(dto, ObtenerUsuarioId());
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        private Guid? ObtenerUsuarioId()
        {
            if (User.Identity?.IsAuthenticated != true)
                return null;
            var claim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier);
            return Guid.TryParse(claim?.Value, out var id) && id != Guid.Empty ? id : null;
        }

        private static bool PareceContenerPan(ProcesarPagoOrdenDto dto)
            => PareceContenerPanToken(dto.Token);

        private static bool PareceContenerPanToken(string? token)
        {
            // Defensa en profundidad: el token no debe ser solo dígitos tipo PAN.
            var raw = token ?? string.Empty;
            var digits = new string(raw.Where(char.IsDigit).ToArray());
            return digits.Length >= 13 && digits.Length <= 19 &&
                   raw.All(c => char.IsDigit(c) || char.IsWhiteSpace(c) || c == '-');
        }
    }
}
