using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TiendaVirtual.Dominio.Servicios.CatalogoXqm;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.CatalogoXqm;
using TiendaVirtual.Intercambio.Dto.Sistema;

namespace TiendaVirtual.Api.Controllers.CatalogoXqm
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "VENDEDOR")]
    public class ProductoController : ControllerBase
    {
        private readonly IProductoServicio _servicio;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ProductoController(IProductoServicio servicio, IHttpContextAccessor httpContextAccessor)
        {
            _servicio = servicio;
            _httpContextAccessor = httpContextAccessor;
        }

        // ─────────────── LISTADO Y DETALLE ───────────────
        [HttpGet("mis-productos")]
        public async Task<ActionResult<ResultadoOperacion<PaginacionRespuestaDto<ProductoDto>>>> MisProductos(
            [FromQuery] int pagina = 1, [FromQuery] int tamanioPagina = 12)
        {
            var uid = ObtenerUsuarioId();
            if (uid == null) return Unauthorized();
            var r = await _servicio.ListarMisProductosAsync(uid.Value, pagina, tamanioPagina);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpGet("mis-productos/{id:int}")]
        public async Task<ActionResult<ResultadoOperacion<ProductoDto>>> ObtenerMiProducto(int id)
        {
            var uid = ObtenerUsuarioId();
            if (uid == null) return Unauthorized();
            var r = await _servicio.ObtenerMiProductoAsync(uid.Value, id);
            return r.Exito ? Ok(r) : NotFound(r);
        }

        // ─────────────── CRUD del producto ───────────────
        [HttpPost]
        public async Task<ActionResult<ResultadoOperacion<ProductoDto>>> Crear([FromBody] CrearProductoDto dto)
        {
            var uid = ObtenerUsuarioId();
            if (uid == null) return Unauthorized();
            var r = await _servicio.CrearAsync(uid.Value, dto);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<ResultadoOperacion<ProductoDto>>> Actualizar(
            int id, [FromBody] ActualizarProductoDto dto)
        {
            var uid = ObtenerUsuarioId();
            if (uid == null) return Unauthorized();
            var r = await _servicio.ActualizarAsync(uid.Value, id, dto);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult<ResultadoOperacion<bool>>> Eliminar(int id)
        {
            var uid = ObtenerUsuarioId();
            if (uid == null) return Unauthorized();
            var r = await _servicio.EliminarAsync(uid.Value, id);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpPost("{id:int}/publicar")]
        public async Task<ActionResult<ResultadoOperacion<bool>>> Publicar(int id)
        {
            var uid = ObtenerUsuarioId();
            if (uid == null) return Unauthorized();
            var r = await _servicio.PublicarAsync(uid.Value, id);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpPost("{id:int}/pausar")]
        public async Task<ActionResult<ResultadoOperacion<bool>>> Pausar(int id)
        {
            var uid = ObtenerUsuarioId();
            if (uid == null) return Unauthorized();
            var r = await _servicio.PausarAsync(uid.Value, id);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        // ─────────────── VARIANTES ───────────────
        [HttpPost("{productoId:int}/variantes")]
        public async Task<ActionResult<ResultadoOperacion<VarianteProductoDto>>> AgregarVariante(
            int productoId, [FromBody] CrearVarianteDto dto)
        {
            var uid = ObtenerUsuarioId();
            if (uid == null) return Unauthorized();
            var r = await _servicio.AgregarVarianteAsync(uid.Value, productoId, dto);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpPut("variantes/{varianteId:int}")]
        public async Task<ActionResult<ResultadoOperacion<VarianteProductoDto>>> ActualizarVariante(
            int varianteId, [FromBody] ActualizarVarianteDto dto)
        {
            var uid = ObtenerUsuarioId();
            if (uid == null) return Unauthorized();
            var r = await _servicio.ActualizarVarianteAsync(uid.Value, varianteId, dto);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpDelete("variantes/{varianteId:int}")]
        public async Task<ActionResult<ResultadoOperacion<bool>>> EliminarVariante(int varianteId)
        {
            var uid = ObtenerUsuarioId();
            if (uid == null) return Unauthorized();
            var r = await _servicio.EliminarVarianteAsync(uid.Value, varianteId);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpPut("variantes/{varianteId:int}/stock")]
        public async Task<ActionResult<ResultadoOperacion<VarianteProductoDto>>> ActualizarStock(
            int varianteId, [FromBody] ActualizarStockDto dto)
        {
            var uid = ObtenerUsuarioId();
            if (uid == null) return Unauthorized();
            var r = await _servicio.ActualizarStockAsync(uid.Value, varianteId, dto);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        // ─────────────── IMÁGENES ───────────────
        [HttpPost("{productoId:int}/imagenes")]
        public async Task<ActionResult<ResultadoOperacion<ImagenProductoDto>>> AgregarImagen(
            int productoId, [FromBody] CrearImagenDto dto)
        {
            var uid = ObtenerUsuarioId();
            if (uid == null) return Unauthorized();
            var r = await _servicio.AgregarImagenAsync(uid.Value, productoId, dto);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpPut("imagenes/{imagenId:int}")]
        public async Task<ActionResult<ResultadoOperacion<ImagenProductoDto>>> ActualizarImagen(
            int imagenId, [FromBody] ActualizarImagenDto dto)
        {
            var uid = ObtenerUsuarioId();
            if (uid == null) return Unauthorized();
            var r = await _servicio.ActualizarImagenAsync(uid.Value, imagenId, dto);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpDelete("imagenes/{imagenId:int}")]
        public async Task<ActionResult<ResultadoOperacion<bool>>> EliminarImagen(int imagenId)
        {
            var uid = ObtenerUsuarioId();
            if (uid == null) return Unauthorized();
            var r = await _servicio.EliminarImagenAsync(uid.Value, imagenId);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        // ─────────────── OFERTAS ───────────────
        [HttpPost("{productoId:int}/ofertas")]
        public async Task<ActionResult<ResultadoOperacion<OfertaDto>>> CrearOferta(
            int productoId, [FromBody] CrearOfertaDto dto)
        {
            var uid = ObtenerUsuarioId();
            if (uid == null) return Unauthorized();
            var r = await _servicio.CrearOfertaAsync(uid.Value, productoId, dto);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpPut("ofertas/{ofertaId:int}")]
        public async Task<ActionResult<ResultadoOperacion<OfertaDto>>> ActualizarOferta(
            int ofertaId, [FromBody] ActualizarOfertaDto dto)
        {
            var uid = ObtenerUsuarioId();
            if (uid == null) return Unauthorized();
            var r = await _servicio.ActualizarOfertaAsync(uid.Value, ofertaId, dto);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpPost("ofertas/{ofertaId:int}/desactivar")]
        public async Task<ActionResult<ResultadoOperacion<bool>>> DesactivarOferta(int ofertaId)
        {
            var uid = ObtenerUsuarioId();
            if (uid == null) return Unauthorized();
            var r = await _servicio.DesactivarOfertaAsync(uid.Value, ofertaId);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        private int? ObtenerUsuarioId()
        {
            var claim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier);
            return int.TryParse(claim?.Value, out var id) ? id : null;
        }
    }
}
