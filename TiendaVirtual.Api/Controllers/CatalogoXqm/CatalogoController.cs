using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TiendaVirtual.Dominio.Servicios.CatalogoXqm;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.CatalogoXqm;
using TiendaVirtual.Intercambio.Dto.Sistema;

namespace TiendaVirtual.Api.Controllers.CatalogoXqm
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class CatalogoController : ControllerBase
    {
        private readonly ICatalogoServicio _servicio;

        public CatalogoController(ICatalogoServicio servicio) => _servicio = servicio;

        [HttpGet]
        public async Task<ActionResult<ResultadoOperacion<PaginacionRespuestaDto<ProductoListadoDto>>>> Listar(
            [FromQuery] FiltrosCatalogoDto filtros)
        {
            var r = await _servicio.ListarAsync(filtros);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpGet("{slug}")]
        public async Task<ActionResult<ResultadoOperacion<ProductoDetalleDto>>> ObtenerPorSlug(string slug)
        {
            var r = await _servicio.ObtenerPorSlugAsync(slug);
            return r.Exito ? Ok(r) : NotFound(r);
        }

        [HttpGet("{slug}/relacionados")]
        public async Task<ActionResult<ResultadoOperacion<List<ProductoListadoDto>>>> ObtenerRelacionados(
            string slug, [FromQuery] int cantidad = 6)
        {
            var r = await _servicio.ObtenerRelacionadosAsync(slug, cantidad);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpGet("del-vendedor/{vendedorId:int}")]
        public async Task<ActionResult<ResultadoOperacion<PaginacionRespuestaDto<ProductoListadoDto>>>> ListarPorVendedor(
            int vendedorId,
            [FromQuery] int pagina = 1,
            [FromQuery] int tamanioPagina = 12)
        {
            var r = await _servicio.ListarPorVendedorAsync(vendedorId, pagina, tamanioPagina);
            return r.Exito ? Ok(r) : BadRequest(r);
        }
    }
}
