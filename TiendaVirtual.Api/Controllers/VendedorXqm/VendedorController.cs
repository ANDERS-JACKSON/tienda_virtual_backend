using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Servicios.VendedorXqm;
using TiendaVirtual.Dominio.Servicios.VentaXqm;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.Sistema;
using TiendaVirtual.Intercambio.Dto.VendedorXqm;
using TiendaVirtual.Intercambio.Dto.VentaXqm;

namespace TiendaVirtual.Api.Controllers.VendedorXqm
{
    [ApiController]
    [Route("api/[controller]")]
    public class VendedorController : ControllerBase
    {
        private readonly IVendedorServicio _servicio;
        private readonly IOrdenServicio _ordenServicio;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public VendedorController(
            IVendedorServicio servicio,
            IOrdenServicio ordenServicio,
            IHttpContextAccessor httpContextAccessor)
        {
            _servicio = servicio;
            _ordenServicio = ordenServicio;
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

        /// <summary>
        /// Guarda logo y/o banner inmediatamente tras subirlos a Cloudinary.
        /// </summary>
        [HttpPatch("mi-perfil/imagenes")]
        [Authorize(Roles = "VENDEDOR")]
        public async Task<ActionResult<ResultadoOperacion<VendedorPerfilDto>>> ActualizarImagenesPerfil(
            [FromBody] ActualizarImagenesPerfilVendedorDto dto)
        {
            var usuarioId = ObtenerUsuarioId();
            if (usuarioId == null) return Unauthorized();
            var r = await _servicio.ActualizarImagenesPerfilAsync(usuarioId.Value, dto);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        /// <summary>Indica si el vendedor puede iniciar el flujo de creación de productos.</summary>
        [HttpGet("elegibilidad-creacion-producto")]
        [Authorize(Roles = "VENDEDOR")]
        public async Task<ActionResult<ResultadoOperacion<ElegibilidadCreacionProductoDto>>> ObtenerElegibilidadCreacionProducto()
        {
            var usuarioId = ObtenerUsuarioId();
            if (usuarioId == null) return Unauthorized();
            var r = await _servicio.ObtenerElegibilidadCreacionProductoAsync(usuarioId.Value);
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

        // ──────────────── HISTORIAS PÚBLICAS (BIOGRAFÍAS) ────────────────
        [HttpGet("historias")]
        [AllowAnonymous]
        public async Task<ActionResult<ResultadoOperacion<PaginacionRespuestaDto<HistoriaPublicaListadoDto>>>> ListarHistorias(
            [FromQuery] int pagina = 1,
            [FromQuery] int tamanioPagina = 12,
            [FromQuery] string? busqueda = null)
        {
            var r = await _servicio.ListarHistoriasPublicasAsync(pagina, tamanioPagina, busqueda);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpGet("historias/{slug}")]
        [AllowAnonymous]
        public async Task<ActionResult<ResultadoOperacion<HistoriaPublicaDetalleDto>>> ObtenerHistoriaPorSlug(string slug)
        {
            var r = await _servicio.ObtenerHistoriaPorSlugAsync(slug);
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

        [HttpGet("mis-pedidos/{subordenId:long}")]
        [Authorize(Roles = "VENDEDOR")]
        public async Task<ActionResult<ResultadoOperacion<PedidoVendedorDetalleDto>>> ObtenerMisPedido(long subordenId)
        {
            var usuarioId = ObtenerUsuarioId();
            if (usuarioId == null) return Unauthorized();
            var r = await _servicio.ObtenerMisPedidoDetalleAsync(usuarioId.Value, subordenId);
            if (!r.Exito)
                return r.Mensaje == "Pedido no encontrado" ? NotFound(r) : BadRequest(r);
            return Ok(r);
        }

        [HttpPost("mis-pedidos/{subordenId:long}/envio")]
        [Authorize(Roles = "VENDEDOR")]
        public async Task<ActionResult<ResultadoOperacion<EnvioDto>>> RegistrarEnvio(
            long subordenId, [FromBody] RegistrarEnvioSubordenDto dto)
        {
            var usuarioId = ObtenerUsuarioId();
            if (usuarioId == null) return Unauthorized();
            var r = await _ordenServicio.RegistrarEnvioSubordenAsync(usuarioId.Value, subordenId, dto);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpPost("mis-pedidos/{subordenId:long}/listo-recoger")]
        [Authorize(Roles = "VENDEDOR")]
        public async Task<ActionResult<ResultadoOperacion<bool>>> MarcarListoParaRecoger(
            long subordenId, [FromBody] MarcarListoParaRecogerDto? dto)
        {
            var usuarioId = ObtenerUsuarioId();
            if (usuarioId == null) return Unauthorized();
            var r = await _ordenServicio.MarcarListoParaRecogerAsync(usuarioId.Value, subordenId, dto);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpPatch("mis-pedidos/{subordenId:long}/estado")]
        [Authorize(Roles = "VENDEDOR")]
        public async Task<ActionResult<ResultadoOperacion<bool>>> CambiarEstadoPedido(
            long subordenId, [FromQuery] TipoEstadoSuborden estado)
        {
            var usuarioId = ObtenerUsuarioId();
            if (usuarioId == null) return Unauthorized();
            var r = await _ordenServicio.CambiarEstadoSubordenAsync(usuarioId.Value, subordenId, estado);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        // ──────────────── ADMIN ────────────────
        [HttpGet("admin")]
        [Authorize(Roles = "ADMIN")]
        public async Task<ActionResult<ResultadoOperacion<PaginacionRespuestaDto<VendedorAdminListadoDto>>>> ListarAdmin(
            [FromQuery] string? busqueda = null,
            [FromQuery] TipoEstadoVendedor? estado = null,
            [FromQuery] bool? conSuscripcion = null,
            [FromQuery] int pagina = 1,
            [FromQuery] int tamanioPagina = 20)
        {
            var r = await _servicio.ListarAdminAsync(busqueda, estado, conSuscripcion, pagina, tamanioPagina);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpGet("admin/{vendedorId:int}")]
        [Authorize(Roles = "ADMIN")]
        public async Task<ActionResult<ResultadoOperacion<VendedorAdminDetalleDto>>> DetalleAdmin(int vendedorId)
        {
            var r = await _servicio.ObtenerAdminDetalleAsync(vendedorId);
            return r.Exito ? Ok(r) : NotFound(r);
        }

        [HttpPost("admin/{vendedorId:int}/suspender")]
        [Authorize(Roles = "ADMIN")]
        public async Task<ActionResult<ResultadoOperacion<bool>>> Suspender(
            int vendedorId, [FromBody] SuspenderVendedorDto dto)
        {
            var r = await _servicio.SuspenderAdminAsync(vendedorId, dto);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpPost("admin/{vendedorId:int}/reactivar")]
        [Authorize(Roles = "ADMIN")]
        public async Task<ActionResult<ResultadoOperacion<bool>>> Reactivar(int vendedorId)
        {
            var r = await _servicio.ReactivarAdminAsync(vendedorId);
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
