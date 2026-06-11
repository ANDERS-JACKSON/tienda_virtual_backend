using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TiendaVirtual.Dominio.Servicios.SoporteXqm;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.Sistema;
using TiendaVirtual.Intercambio.Dto.SoporteXqm;

namespace TiendaVirtual.Api.Controllers.SoporteXqm
{
    [ApiController]
    [Route("api/[controller]")]
    public class ResenaController : ControllerBase
    {
        private readonly IResenaServicio _servicio;
        private readonly IHttpContextAccessor _http;

        public ResenaController(IResenaServicio servicio, IHttpContextAccessor http)
        {
            _servicio = servicio;
            _http = http;
        }

        [HttpPost("producto")]
        [Authorize(Roles = "CLIENTE")]
        public async Task<IActionResult> CrearProducto([FromBody] CrearResenaProductoDto dto)
        {
            var uid = ObtenerUsuarioId();
            if (uid == null) return Unauthorized();
            var r = await _servicio.CrearResenaProductoAsync(uid.Value, dto);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpPost("vendedor")]
        [Authorize(Roles = "CLIENTE")]
        public async Task<IActionResult> CrearVendedor([FromBody] CrearResenaVendedorDto dto)
        {
            var uid = ObtenerUsuarioId();
            if (uid == null) return Unauthorized();
            var r = await _servicio.CrearResenaVendedorAsync(uid.Value, dto);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpPost("producto/{resenaId:long}/responder")]
        [Authorize(Roles = "VENDEDOR")]
        public async Task<IActionResult> Responder(long resenaId, [FromBody] ResponderResenaDto dto)
        {
            var uid = ObtenerUsuarioId();
            if (uid == null) return Unauthorized();
            var r = await _servicio.ResponderResenaProductoAsync(uid.Value, resenaId, dto);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpGet("producto/{productoId:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> ListarPorProducto(
            int productoId, [FromQuery] int pagina = 1, [FromQuery] int tamanioPagina = 20)
        {
            var r = await _servicio.ListarPorProductoAsync(productoId, pagina, tamanioPagina);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpGet("vendedor/{vendedorId:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> ListarPorVendedor(
            int vendedorId, [FromQuery] int pagina = 1, [FromQuery] int tamanioPagina = 20)
        {
            var r = await _servicio.ListarPorVendedorAsync(vendedorId, pagina, tamanioPagina);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpGet("mis-pendientes")]
        [Authorize(Roles = "CLIENTE")]
        public async Task<IActionResult> MisPendientes()
        {
            var uid = ObtenerUsuarioId();
            if (uid == null) return Unauthorized();
            var r = await _servicio.ObtenerPendientesAsync(uid.Value);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        private int? ObtenerUsuarioId()
        {
            var claim = _http.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier);
            return int.TryParse(claim?.Value, out var id) ? id : null;
        }
    }
}
