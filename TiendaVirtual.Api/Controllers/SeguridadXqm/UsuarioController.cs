using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TiendaVirtual.Dominio.Servicios.SeguridadXqm;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.SeguridadXqm;
using TiendaVirtual.Intercambio.Dto.Sistema;

namespace TiendaVirtual.Api.Controllers.SeguridadXqm
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsuarioController : ControllerBase
    {
        private readonly IUsuarioAdminServicio _servicio;
        private readonly IUsuarioPerfilServicio _perfilServicio;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UsuarioController(
            IUsuarioAdminServicio servicio,
            IUsuarioPerfilServicio perfilServicio,
            IHttpContextAccessor httpContextAccessor)
        {
            _servicio = servicio;
            _perfilServicio = perfilServicio;
            _httpContextAccessor = httpContextAccessor;
        }

        // ──────────────── PERFIL PROPIO ────────────────
        [HttpGet("mi-perfil")]
        [Authorize]
        public async Task<ActionResult<ResultadoOperacion<UsuarioPerfilDto>>> ObtenerMiPerfil()
        {
            var usuarioId = ObtenerUsuarioId();
            if (usuarioId == null) return Unauthorized();
            var r = await _perfilServicio.ObtenerMiPerfilAsync(usuarioId.Value);
            return r.Exito ? Ok(r) : NotFound(r);
        }

        [HttpPut("mi-perfil")]
        [Authorize]
        public async Task<ActionResult<ResultadoOperacion<UsuarioPerfilDto>>> ActualizarMiPerfil(
            [FromBody] ActualizarMisDatosPersonalesDto dto)
        {
            var usuarioId = ObtenerUsuarioId();
            if (usuarioId == null) return Unauthorized();
            var r = await _perfilServicio.ActualizarMisDatosAsync(usuarioId.Value, dto);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        // ──────────────── ADMIN ────────────────
        [HttpGet("admin")]
        [Authorize(Roles = "ADMIN")]
        public async Task<ActionResult<ResultadoOperacion<PaginacionRespuestaDto<UsuarioAdminListadoDto>>>> Listar(
            [FromQuery] string? busqueda = null,
            [FromQuery] int? rolId = null,
            [FromQuery] string? estado = null,
            [FromQuery] int pagina = 1,
            [FromQuery] int tamanioPagina = 20)
        {
            var r = await _servicio.ListarAsync(busqueda, rolId, estado, pagina, tamanioPagina);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpGet("admin/{id:int}")]
        [Authorize(Roles = "ADMIN")]
        public async Task<ActionResult<ResultadoOperacion<UsuarioAdminDetalleDto>>> Detalle(int id)
        {
            var r = await _servicio.ObtenerDetalleAsync(id);
            return r.Exito ? Ok(r) : NotFound(r);
        }

        [HttpPost("admin/{id:int}/activar")]
        [Authorize(Roles = "ADMIN")]
        public async Task<ActionResult<ResultadoOperacion<bool>>> Activar(int id)
        {
            var r = await _servicio.ActivarAsync(id);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpPost("admin/{id:int}/desactivar")]
        [Authorize(Roles = "ADMIN")]
        public async Task<ActionResult<ResultadoOperacion<bool>>> Desactivar(int id)
        {
            var r = await _servicio.DesactivarAsync(id);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpPost("admin/{id:int}/roles/asignar")]
        [Authorize(Roles = "ADMIN")]
        public async Task<ActionResult<ResultadoOperacion<bool>>> AsignarRol(int id, [FromBody] AsignarRolDto dto)
        {
            var r = await _servicio.AsignarRolAsync(id, dto.RolId);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpPost("admin/{id:int}/roles/quitar")]
        [Authorize(Roles = "ADMIN")]
        public async Task<ActionResult<ResultadoOperacion<bool>>> QuitarRol(int id, [FromBody] AsignarRolDto dto)
        {
            var uid = ObtenerUsuarioId();
            if (uid == null) return Unauthorized();
            var r = await _servicio.QuitarRolAsync(id, dto.RolId, uid.Value);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpPost("admin/{id:int}/resetear-clave")]
        [Authorize(Roles = "ADMIN")]
        public async Task<ActionResult<ResultadoOperacion<bool>>> ResetearClave(int id)
        {
            var r = await _servicio.ResetearClaveAsync(id);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        private int? ObtenerUsuarioId()
        {
            var claim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier);
            return int.TryParse(claim?.Value, out var id) ? id : null;
        }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class RolController : ControllerBase
    {
        private readonly IUsuarioAdminServicio _servicio;

        public RolController(IUsuarioAdminServicio servicio) => _servicio = servicio;

        [HttpGet]
        [Authorize(Roles = "ADMIN")]
        public async Task<ActionResult<ResultadoOperacion<List<RolDto>>>> Listar()
        {
            var r = await _servicio.ListarRolesAsync();
            return r.Exito ? Ok(r) : BadRequest(r);
        }
    }
}
