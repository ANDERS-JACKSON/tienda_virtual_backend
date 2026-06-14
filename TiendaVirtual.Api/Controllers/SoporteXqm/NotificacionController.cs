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
    public class NotificacionController : ControllerBase
    {
        private readonly INotificacionServicio _servicio;
        private readonly IHttpContextAccessor _http;

        public NotificacionController(INotificacionServicio servicio, IHttpContextAccessor http)
        {
            _servicio = servicio;
            _http = http;
        }

        [HttpGet]
        public async Task<ActionResult<ResultadoOperacion<PaginacionRespuestaDto<NotificacionDto>>>> Listar(
            [FromQuery] int pagina = 1, [FromQuery] int tamanioPagina = 20)
        {
            var uid = ObtenerUsuarioId();
            if (uid == null) return Unauthorized();
            var r = await _servicio.ListarMisAsync(uid.Value, pagina, tamanioPagina);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpGet("no-leidas/contador")]
        public async Task<ActionResult<ResultadoOperacion<int>>> Contador()
        {
            var uid = ObtenerUsuarioId();
            if (uid == null) return Unauthorized();
            var r = await _servicio.ContarNoLeidasAsync(uid.Value);
            return r.Exito
                ? Ok(r)
                : StatusCode(StatusCodes.Status500InternalServerError, r);
        }

        [HttpPost("{id:long}/leer")]
        public async Task<ActionResult<ResultadoOperacion<bool>>> Leer(long id)
        {
            var uid = ObtenerUsuarioId();
            if (uid == null) return Unauthorized();
            var r = await _servicio.MarcarLeidaAsync(uid.Value, id);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpPost("leer-todas")]
        public async Task<ActionResult<ResultadoOperacion<int>>> LeerTodas()
        {
            var uid = ObtenerUsuarioId();
            if (uid == null) return Unauthorized();
            var r = await _servicio.MarcarTodasLeidasAsync(uid.Value);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        private int? ObtenerUsuarioId()
        {
            var claim = _http.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier);
            return int.TryParse(claim?.Value, out var id) ? id : null;
        }
    }
}
