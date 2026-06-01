using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Servicios.VendedorXqm;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.Sistema;
using TiendaVirtual.Intercambio.Dto.VendedorXqm;

namespace TiendaVirtual.Api.Controllers.VendedorXqm
{
    [ApiController]
    [Route("api/[controller]")]
    public class VendedorController : ControllerBase
    {
        private readonly IVendedorServicio _servicio;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public VendedorController(IVendedorServicio servicio, IHttpContextAccessor httpContextAccessor)
        {
            _servicio = servicio;
            _httpContextAccessor = httpContextAccessor;
        }

        // ──────────────── PERFIL PROPIO ────────────────
        [HttpGet("mi-perfil")]
        [Authorize(Roles = "VENDEDOR")]
        public async Task<ActionResult<ResultadoOperacion<VendedorPerfilDto>>> ObtenerMiPerfil()
        {
            var usuarioId = ObtenerUsuarioId();
            if (usuarioId == null) return Unauthorized();
            var r = await _servicio.ObtenerMiPerfilAsync(usuarioId.Value);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpPut("mi-perfil")]
        [Authorize(Roles = "VENDEDOR")]
        public async Task<ActionResult<ResultadoOperacion<VendedorPerfilDto>>> ActualizarMiPerfil(
            [FromBody] ActualizarPerfilVendedorDto dto)
        {
            var usuarioId = ObtenerUsuarioId();
            if (usuarioId == null) return Unauthorized();
            var r = await _servicio.ActualizarMiPerfilAsync(usuarioId.Value, dto);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        // ──────────────── SOLICITUD DE VERIFICACIÓN ────────────────
        [HttpPost("mi-solicitud-verificacion")]
        [Authorize(Roles = "VENDEDOR")]
        public async Task<ActionResult<ResultadoOperacion<SolicitudVerificacionDto>>> EnviarSolicitud(
            [FromBody] EnviarSolicitudVerificacionDto dto)
        {
            var usuarioId = ObtenerUsuarioId();
            if (usuarioId == null) return Unauthorized();
            var r = await _servicio.EnviarSolicitudVerificacionAsync(usuarioId.Value, dto);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpGet("mi-solicitud-verificacion")]
        [Authorize(Roles = "VENDEDOR")]
        public async Task<ActionResult<ResultadoOperacion<SolicitudVerificacionDto?>>> ObtenerMiSolicitud()
        {
            var usuarioId = ObtenerUsuarioId();
            if (usuarioId == null) return Unauthorized();
            var r = await _servicio.ObtenerMiSolicitudActualAsync(usuarioId.Value);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        // ──────────────── RESOLUCIÓN (ADMIN / VERIFICADOR) ────────────────
        [HttpGet("solicitudes-pendientes")]
        [Authorize(Roles = "ADMIN,VERIFICADOR")]
        public async Task<ActionResult<ResultadoOperacion<PaginacionRespuestaDto<SolicitudVerificacionDto>>>> ListarPendientes(
            [FromQuery] int pagina = 1, [FromQuery] int tamanioPagina = 20)
        {
            var r = await _servicio.ListarSolicitudesPendientesAsync(pagina, tamanioPagina);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpPost("solicitudes/{solicitudId:int}/aprobar")]
        [Authorize(Roles = "ADMIN,VERIFICADOR")]
        public async Task<ActionResult<ResultadoOperacion<bool>>> Aprobar(
            int solicitudId, [FromBody] ResolverSolicitudDto dto)
        {
            var usuarioId = ObtenerUsuarioId();
            if (usuarioId == null) return Unauthorized();
            var r = await _servicio.AprobarSolicitudAsync(solicitudId, usuarioId.Value, dto);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpPost("solicitudes/{solicitudId:int}/rechazar")]
        [Authorize(Roles = "ADMIN,VERIFICADOR")]
        public async Task<ActionResult<ResultadoOperacion<bool>>> Rechazar(
            int solicitudId, [FromBody] ResolverSolicitudDto dto)
        {
            var usuarioId = ObtenerUsuarioId();
            if (usuarioId == null) return Unauthorized();
            var r = await _servicio.RechazarSolicitudAsync(solicitudId, usuarioId.Value, dto);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        // ──────────────── LISTADO PÚBLICO DE TIENDAS ────────────────
        [HttpGet("tiendas")]
        [AllowAnonymous]
        public async Task<ActionResult<ResultadoOperacion<PaginacionRespuestaDto<TiendaPublicaDto>>>> ListarTiendas(
            [FromQuery] int pagina = 1,
            [FromQuery] int tamanioPagina = 12,
            [FromQuery] string? busqueda = null)
        {
            var r = await _servicio.ListarTiendasPublicasAsync(pagina, tamanioPagina, busqueda);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpGet("tiendas/{slug}")]
        [AllowAnonymous]
        public async Task<ActionResult<ResultadoOperacion<TiendaPublicaDto>>> ObtenerTiendaPorSlug(string slug)
        {
            var r = await _servicio.ObtenerTiendaPorSlugAsync(slug);
            return r.Exito ? Ok(r) : NotFound(r);
        }

        // ──────────────── PEDIDOS DEL VENDEDOR ────────────────
        [HttpGet("mis-pedidos")]
        [Authorize(Roles = "VENDEDOR")]
        public async Task<ActionResult<ResultadoOperacion<PaginacionRespuestaDto<PedidoVendedorDto>>>> MisPedidos(
            [FromQuery] TipoEstadoSuborden? estado = null,
            [FromQuery] int pagina = 1,
            [FromQuery] int tamanioPagina = 20)
        {
            var usuarioId = ObtenerUsuarioId();
            if (usuarioId == null) return Unauthorized();
            var r = await _servicio.ListarMisPedidosAsync(usuarioId.Value, estado, pagina, tamanioPagina);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        // ──────────────── HELPER ────────────────
        private int? ObtenerUsuarioId()
        {
            var claim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier);
            return int.TryParse(claim?.Value, out var id) ? id : null;
        }
    }
}
