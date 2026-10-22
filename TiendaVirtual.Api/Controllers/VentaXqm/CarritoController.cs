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
    public class CarritoController : ControllerBase
    {
        private readonly ICarritoServicio _servicio;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CarritoController(ICarritoServicio servicio, IHttpContextAccessor httpContextAccessor)
        {
            _servicio = servicio;
            _httpContextAccessor = httpContextAccessor;
        }

        [HttpGet]
        public async Task<ActionResult<ResultadoOperacion<CarritoDto>>> Obtener()
        {
            var uid = ObtenerUsuarioId();
            if (uid == null) return Unauthorized();
            var r = await _servicio.ObtenerMiCarritoAsync(uid.Value);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpPost("items")]
        public async Task<ActionResult<ResultadoOperacion<CarritoDto>>> Agregar(
            [FromBody] AgregarItemCarritoDto dto)
        {
            var uid = ObtenerUsuarioId();
            if (uid == null) return Unauthorized();
            var r = await _servicio.AgregarItemAsync(uid.Value, dto);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpPut("items/{itemId:int}")]
        public async Task<ActionResult<ResultadoOperacion<CarritoDto>>> Actualizar(
            int itemId, [FromBody] ActualizarItemCarritoDto dto)
        {
            var uid = ObtenerUsuarioId();
            if (uid == null) return Unauthorized();
            var r = await _servicio.ActualizarItemAsync(uid.Value, itemId, dto);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpDelete("items/{itemId:int}")]
        public async Task<ActionResult<ResultadoOperacion<CarritoDto>>> Quitar(int itemId)
        {
            var uid = ObtenerUsuarioId();
            if (uid == null) return Unauthorized();
            var r = await _servicio.QuitarItemAsync(uid.Value, itemId);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpDelete]
        public async Task<ActionResult<ResultadoOperacion<bool>>> Vaciar()
        {
            var uid = ObtenerUsuarioId();
            if (uid == null) return Unauthorized();
            var r = await _servicio.VaciarAsync(uid.Value);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        /// <summary>
        /// Dice si el código existe y si exige iniciar sesión. No lo aplica.
        /// </summary>
        [AllowAnonymous]
        [EnableRateLimiting("cotizar")]
        [HttpPost("cupon/revisar")]
        public async Task<ActionResult<ResultadoOperacion<RevisionCuponCarritoDto>>> RevisarCupon(
            [FromBody] RevisarCuponCarritoDto dto)
        {
            var r = await _servicio.RevisarCuponAsync(dto ?? new RevisarCuponCarritoDto());
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpPost("cupon")]
        public async Task<ActionResult<ResultadoOperacion<CarritoDto>>> AplicarCupon(
            [FromBody] AplicarCuponCarritoDto dto)
        {
            var uid = ObtenerUsuarioId();
            if (uid == null) return Unauthorized();
            var r = await _servicio.AplicarCuponAsync(uid.Value, dto);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        /// <summary>
        /// Precios y stock de un carrito guardado en el navegador, sin cuenta.
        /// </summary>
        [AllowAnonymous]
        [EnableRateLimiting("cotizar")]
        [HttpPost("cotizar")]
        public async Task<ActionResult<ResultadoOperacion<CarritoDto>>> Cotizar(
            [FromBody] CotizarCarritoDto dto)
        {
            var r = await _servicio.CotizarAsync(dto ?? new CotizarCarritoDto());
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        /// <summary>
        /// Pasa el carrito del navegador a la cuenta. Las cantidades se suman.
        /// </summary>
        [HttpPost("fusionar")]
        public async Task<ActionResult<ResultadoOperacion<ResultadoFusionCarritoDto>>> Fusionar(
            [FromBody] CotizarCarritoDto dto)
        {
            var uid = ObtenerUsuarioId();
            if (uid == null) return Unauthorized();
            var r = await _servicio.FusionarAsync(uid.Value, dto ?? new CotizarCarritoDto());
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpDelete("cupon")]
        public async Task<ActionResult<ResultadoOperacion<CarritoDto>>> QuitarCupon()
        {
            var uid = ObtenerUsuarioId();
            if (uid == null) return Unauthorized();
            var r = await _servicio.QuitarCuponAsync(uid.Value);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        private Guid? ObtenerUsuarioId()
        {
            var claim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier);
            return Guid.TryParse(claim?.Value, out var id) && id != Guid.Empty ? id : null;
        }
    }
}
