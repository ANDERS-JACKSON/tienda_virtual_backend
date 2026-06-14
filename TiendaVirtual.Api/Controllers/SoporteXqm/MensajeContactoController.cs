using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Servicios.SoporteXqm;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.Sistema;
using TiendaVirtual.Intercambio.Dto.SoporteXqm;

namespace TiendaVirtual.Api.Controllers.SoporteXqm
{
    [ApiController]
    [Route("api/contacto")]
    public class MensajeContactoController : ControllerBase
    {
        private readonly IMensajeContactoServicio _servicio;
        private readonly IHttpContextAccessor _http;

        public MensajeContactoController(IMensajeContactoServicio servicio, IHttpContextAccessor http)
        {
            _servicio = servicio;
            _http = http;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult<ResultadoOperacion<long>>> Crear([FromBody] CrearMensajeContactoDto dto)
        {
            var usuarioId = ObtenerUsuarioIdOpcional();
            var r = await _servicio.CrearAsync(dto, usuarioId);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpGet("admin")]
        [Authorize(Roles = "ADMIN")]
        public async Task<ActionResult<ResultadoOperacion<PaginacionRespuestaDto<MensajeContactoListadoDto>>>> Listar(
            [FromQuery] int pagina = 1,
            [FromQuery] int tamanio = 20,
            [FromQuery] int? estado = null,
            [FromQuery] string? busqueda = null)
        {
            var r = await _servicio.ListarAsync(pagina, tamanio, estado, busqueda);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpGet("admin/contadores")]
        [Authorize(Roles = "ADMIN")]
        public async Task<ActionResult<ResultadoOperacion<ContadorMensajesContactoDto>>> Contadores()
        {
            var r = await _servicio.ContarNoLeidosAsync();
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpGet("admin/{id:long}")]
        [Authorize(Roles = "ADMIN")]
        public async Task<ActionResult<ResultadoOperacion<MensajeContactoDetalleDto>>> Obtener(long id)
        {
            var r = await _servicio.ObtenerDetalleAsync(id);
            return r.Exito ? Ok(r) : NotFound(r);
        }

        [HttpPost("admin/{id:long}/responder")]
        [Authorize(Roles = "ADMIN")]
        public async Task<ActionResult<ResultadoOperacion<bool>>> Responder(
            long id, [FromBody] ResponderMensajeContactoDto dto)
        {
            var adminId = ObtenerUsuarioId();
            if (adminId == null) return Unauthorized();
            var r = await _servicio.ResponderAsync(id, adminId.Value, dto);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpPatch("admin/{id:long}/estado")]
        [Authorize(Roles = "ADMIN")]
        public async Task<ActionResult<ResultadoOperacion<bool>>> CambiarEstado(
            long id, [FromBody] CambiarEstadoMensajeContactoDto dto)
        {
            var adminId = ObtenerUsuarioId();
            if (adminId == null) return Unauthorized();
            if (!Enum.IsDefined(typeof(TipoEstadoContacto), dto.Estado))
                return BadRequest(ResultadoOperacion<bool>.SetError("Estado inválido."));
            var r = await _servicio.CambiarEstadoAsync(id, adminId.Value, (TipoEstadoContacto)dto.Estado);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        private int? ObtenerUsuarioId()
        {
            var claim = _http.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier);
            return int.TryParse(claim?.Value, out var id) ? id : null;
        }

        private int? ObtenerUsuarioIdOpcional()
        {
            if (_http.HttpContext?.User?.Identity?.IsAuthenticated != true)
                return null;
            return ObtenerUsuarioId();
        }
    }
}
