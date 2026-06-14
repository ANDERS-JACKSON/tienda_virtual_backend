using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TiendaVirtual.Dominio.Servicios.CatalogoXqm;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.CatalogoXqm;

namespace TiendaVirtual.Api.Controllers.CatalogoXqm
{
    [ApiController]
    [Route("api/producto-destacado")]
    public class ProductoDestacadoController : ControllerBase
    {
        private readonly IProductoDestacadoServicio _servicio;

        public ProductoDestacadoController(IProductoDestacadoServicio servicio) =>
            _servicio = servicio;

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<ResultadoOperacion<List<ProductoDestacadoPublicoDto>>>> ListarPublico()
        {
            var r = await _servicio.ListarPublicoAsync();
            return r.Exito
                ? Ok(r)
                : StatusCode(StatusCodes.Status500InternalServerError, r);
        }

        [HttpGet("admin")]
        [Authorize(Roles = "ADMIN")]
        public async Task<ActionResult<ResultadoOperacion<List<ProductoDestacadoAdminDto>>>> ListarAdmin()
        {
            var r = await _servicio.ListarAdminAsync();
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpPost("admin")]
        [Authorize(Roles = "ADMIN")]
        public async Task<ActionResult<ResultadoOperacion<ProductoDestacadoAdminDto>>> Agregar(
            [FromBody] AgregarProductoDestacadoDto dto)
        {
            var r = await _servicio.AgregarAsync(dto);
            if (!r.Exito) return BadRequest(r);
            return Ok(r);
        }

        [HttpDelete("admin/{id:int}")]
        [Authorize(Roles = "ADMIN")]
        public async Task<ActionResult<ResultadoOperacion<bool>>> Eliminar(int id)
        {
            var r = await _servicio.EliminarAsync(id);
            if (!r.Exito)
            {
                if (r.Mensaje == "No encontrado") return NotFound(r);
                return BadRequest(r);
            }
            return Ok(r);
        }

        [HttpPut("admin/reordenar")]
        [Authorize(Roles = "ADMIN")]
        public async Task<ActionResult<ResultadoOperacion<bool>>> Reordenar(
            [FromBody] ReordenarProductosDestacadosDto dto)
        {
            var r = await _servicio.ReordenarAsync(dto);
            if (!r.Exito) return BadRequest(r);
            return Ok(r);
        }
    }
}
