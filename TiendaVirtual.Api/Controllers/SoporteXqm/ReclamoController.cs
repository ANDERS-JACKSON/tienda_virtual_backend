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
    [Authorize]
    public class ReclamoController : ControllerBase
    {
        private readonly IReclamoServicio _servicio;
        private readonly IHttpContextAccessor _http;

        public ReclamoController(IReclamoServicio servicio, IHttpContextAccessor http)
        {
            _servicio = servicio;
            _http = http;
        }

        [HttpPost]
        [Authorize(Roles = "CLIENTE,VENDEDOR")]
        public async Task<IActionResult> Abrir([FromBody] AbrirReclamoDto dto)
        {
            var uid = ObtenerUsuarioId();
            if (uid == null) return Unauthorized();
            var r = await _servicio.AbrirAsync(uid.Value, dto);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpGet("{id:long}")]
        public async Task<IActionResult> Obtener(long id)
        {
            var uid = ObtenerUsuarioId();
            if (uid == null) return Unauthorized();
            var r = await _servicio.ObtenerDetalleAsync(uid.Value, id);
            return r.Exito ? Ok(r) : NotFound(r);
        }

        [HttpPost("{id:long}/mensajes")]
        public async Task<IActionResult> AgregarMensaje(long id, [FromBody] AgregarMensajeReclamoDto dto)
        {
            var uid = ObtenerUsuarioId();
            if (uid == null) return Unauthorized();
            var r = await _servicio.AgregarMensajeAsync(uid.Value, id, dto);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpPost("{id:long}/resolver")]
        [Authorize(Roles = "ADMIN,VERIFICADOR")]
        public async Task<IActionResult> Resolver(long id, [FromBody] ResolverReclamoDto dto)
        {
            var uid = ObtenerUsuarioId();
            if (uid == null) return Unauthorized();
            var r = await _servicio.ResolverAsync(uid.Value, id, dto);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpGet("mis-reclamos")]
        [Authorize(Roles = "CLIENTE,VENDEDOR")]
        public async Task<IActionResult> Mios([FromQuery] int pagina = 1, [FromQuery] int tamanioPagina = 20)
        {
            var uid = ObtenerUsuarioId();
            if (uid == null) return Unauthorized();
            var r = await _servicio.ListarMisAsync(uid.Value, pagina, tamanioPagina);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpGet("mis-reclamos-recibidos")]
        [Authorize(Roles = "VENDEDOR")]
        public async Task<IActionResult> Recibidos([FromQuery] int pagina = 1, [FromQuery] int tamanioPagina = 20)
        {
            var uid = ObtenerUsuarioId();
            if (uid == null) return Unauthorized();
            var r = await _servicio.ListarRecibidosAsync(uid.Value, pagina, tamanioPagina);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpGet("admin")]
        [Authorize(Roles = "ADMIN,VERIFICADOR")]
        public async Task<IActionResult> Admin(
            [FromQuery] int? estado, [FromQuery] int pagina = 1, [FromQuery] int tamanioPagina = 20)
        {
            var r = await _servicio.ListarAdminAsync(estado, pagina, tamanioPagina);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        private int? ObtenerUsuarioId()
        {
            var claim = _http.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier);
            return int.TryParse(claim?.Value, out var id) ? id : null;
        }
    }
}
