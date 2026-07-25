using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TiendaVirtual.Api.Seguridad;
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

        public ReclamoController(IReclamoServicio servicio)
        {
            _servicio = servicio;
        }

        [HttpPost]
        [Authorize(Roles = "CLIENTE,VENDEDOR,ADMIN,VERIFICADOR")]
        public async Task<IActionResult> Abrir([FromBody] AbrirReclamoDto dto)
        {
            var uid = User.ObtenerUsuarioId();
            if (uid == null) return Unauthorized();
            var r = await _servicio.AbrirAsync(uid.Value, dto);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        /// <summary>
        /// Detalle con control de acceso en servicio (comprador, vendedor de la suborden o admin).
        /// </summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> Obtener(Guid id)
        {
            var uid = User.ObtenerUsuarioId();
            if (uid == null) return Unauthorized();
            var r = await _servicio.ObtenerDetalleAsync(uid.Value, id);
            return r.Exito ? Ok(r) : NotFound(r);
        }

        [HttpPost("{id:guid}/mensajes")]
        public async Task<IActionResult> AgregarMensaje(Guid id, [FromBody] AgregarMensajeReclamoDto dto)
        {
            var uid = User.ObtenerUsuarioId();
            if (uid == null) return Unauthorized();
            var r = await _servicio.AgregarMensajeAsync(uid.Value, id, dto);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpPost("{id:guid}/resolver")]
        [Authorize(Roles = "ADMIN,VERIFICADOR")]
        public async Task<IActionResult> Resolver(Guid id, [FromBody] ResolverReclamoDto dto)
        {
            var uid = User.ObtenerUsuarioId();
            if (uid == null) return Unauthorized();
            var r = await _servicio.ResolverAsync(uid.Value, id, dto);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpGet("mis-reclamos")]
        [Authorize(Roles = "CLIENTE,VENDEDOR,ADMIN,VERIFICADOR")]
        public async Task<IActionResult> Mios([FromQuery] int pagina = 1, [FromQuery] int tamanioPagina = 20)
        {
            var uid = User.ObtenerUsuarioId();
            if (uid == null) return Unauthorized();
            var r = await _servicio.ListarMisAsync(uid.Value, pagina, tamanioPagina);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpGet("mis-reclamos-recibidos")]
        [Authorize(Roles = "VENDEDOR")]
        public async Task<IActionResult> Recibidos([FromQuery] int pagina = 1, [FromQuery] int tamanioPagina = 20)
        {
            var uid = User.ObtenerUsuarioId();
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
    }
}
