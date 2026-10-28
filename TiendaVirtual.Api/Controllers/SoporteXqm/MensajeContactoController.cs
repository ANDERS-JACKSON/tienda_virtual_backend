using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Servicios.SoporteXqm;
using TiendaVirtual.Dominio.Servicios.Sistema;
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
        private readonly ITurnstileService _turnstile;

        public MensajeContactoController(
            IMensajeContactoServicio servicio, IHttpContextAccessor http, ITurnstileService turnstile)
        {
            _servicio = servicio;
            _http = http;
            _turnstile = turnstile;
        }

        [HttpPost]
        [AllowAnonymous]
        [EnableRateLimiting("contacto")]
        public async Task<ActionResult<ResultadoOperacion<Guid>>> Crear([FromBody] CrearMensajeContactoDto dto)
        {
            // El honeypot ("Sitio") ya lo valida el servicio (CrearAsync). Aquí sólo
            // agregamos la verificación de Turnstile (A4+M12).
            var ip = _http.HttpContext?.Connection.RemoteIpAddress?.ToString();
            if (!await _turnstile.ValidarAsync(dto?.TurnstileToken, ip))
            {
                return BadRequest(ResultadoOperacion<Guid>.SetError(
                    "No pudimos verificar que eres una persona. Recarga la página e inténtalo de nuevo."));
            }

            var usuarioId = ObtenerUsuarioIdOpcional();
            var r = await _servicio.CrearAsync(dto!, usuarioId);
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

        [HttpGet("admin/{id:guid}")]
        [Authorize(Roles = "ADMIN")]
        public async Task<ActionResult<ResultadoOperacion<MensajeContactoDetalleDto>>> Obtener(Guid id)
        {
            var r = await _servicio.ObtenerDetalleAsync(id);
            return r.Exito ? Ok(r) : NotFound(r);
        }

        [HttpPost("admin/{id:guid}/responder")]
        [Authorize(Roles = "ADMIN")]
        public async Task<ActionResult<ResultadoOperacion<bool>>> Responder(
            Guid id, [FromBody] ResponderMensajeContactoDto dto)
        {
            var adminId = ObtenerUsuarioId();
            if (adminId == null) return Unauthorized();
            var r = await _servicio.ResponderAsync(id, adminId.Value, dto);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpPatch("admin/{id:guid}/estado")]
        [Authorize(Roles = "ADMIN")]
        public async Task<ActionResult<ResultadoOperacion<bool>>> CambiarEstado(
            Guid id, [FromBody] CambiarEstadoMensajeContactoDto dto)
        {
            var adminId = ObtenerUsuarioId();
            if (adminId == null) return Unauthorized();
            if (!Enum.IsDefined(typeof(TipoEstadoContacto), dto.Estado))
                return BadRequest(ResultadoOperacion<bool>.SetError("Estado inválido."));
            var r = await _servicio.CambiarEstadoAsync(id, adminId.Value, (TipoEstadoContacto)dto.Estado);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        private Guid? ObtenerUsuarioId()
        {
            var claim = _http.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier);
            return Guid.TryParse(claim?.Value, out var id) && id != Guid.Empty ? id : null;
        }

        private Guid? ObtenerUsuarioIdOpcional()
        {
            if (_http.HttpContext?.User?.Identity?.IsAuthenticated != true)
                return null;
            return ObtenerUsuarioId();
        }
    }
}
