using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TiendaVirtual.Dominio.Servicios.CatalogoXqm;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.CatalogoXqm;
using TiendaVirtual.Intercambio.Dto.Sistema;

namespace TiendaVirtual.Api.Controllers.CatalogoXqm
{
    [ApiController]
    [Route("api/Producto")]
    [Authorize(Roles = "ADMIN")]
    public class ProductoAdminController : ControllerBase
    {
        private readonly IProductoServicio _servicio;

        public ProductoAdminController(IProductoServicio servicio) => _servicio = servicio;

        [HttpGet("admin")]
        public async Task<ActionResult<ResultadoOperacion<PaginacionRespuestaDto<ProductoAdminListadoDto>>>> Listar(
            [FromQuery] string? busqueda = null,
            [FromQuery] int? vendedorId = null,
            [FromQuery] int? categoriaId = null,
            [FromQuery] string? estado = null,
            [FromQuery] int pagina = 1,
            [FromQuery] int tamanioPagina = 20)
        {
            var r = await _servicio.ListarAdminAsync(busqueda, vendedorId, categoriaId, estado, pagina, tamanioPagina);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpPost("admin/{id:int}/ocultar")]
        public async Task<ActionResult<ResultadoOperacion<bool>>> Ocultar(int id, [FromBody] OcultarProductoDto dto)
        {
            var r = await _servicio.OcultarAdminAsync(id, dto);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpPost("admin/{id:int}/restaurar")]
        public async Task<ActionResult<ResultadoOperacion<bool>>> Restaurar(int id)
        {
            var r = await _servicio.RestaurarAdminAsync(id);
            return r.Exito ? Ok(r) : BadRequest(r);
        }
    }
}
