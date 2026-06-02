using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TiendaVirtual.Dominio.Servicios.CatalogoXqm;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.CatalogoXqm;

namespace TiendaVirtual.Api.Controllers.CatalogoXqm
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriaController : ControllerBase
    {
        private readonly ICategoriaServicio _servicio;

        public CategoriaController(ICategoriaServicio servicio) => _servicio = servicio;

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<ResultadoOperacion<List<CategoriaDto>>>> Listar()
        {
            var r = await _servicio.ListarAsync();
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpGet("arbol")]
        [AllowAnonymous]
        public async Task<ActionResult<ResultadoOperacion<List<CategoriaArbolDto>>>> ObtenerArbol()
        {
            var r = await _servicio.ObtenerArbolAsync();
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<ActionResult<ResultadoOperacion<CategoriaDto>>> ObtenerPorId(int id)
        {
            var r = await _servicio.ObtenerPorIdAsync(id);
            return r.Exito ? Ok(r) : NotFound(r);
        }

        [HttpPost]
        [Authorize(Roles = "ADMIN")]
        public async Task<ActionResult<ResultadoOperacion<CategoriaDto>>> Crear([FromBody] CrearCategoriaDto dto)
        {
            var r = await _servicio.CrearAsync(dto);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "ADMIN")]
        public async Task<ActionResult<ResultadoOperacion<CategoriaDto>>> Actualizar(
            int id, [FromBody] ActualizarCategoriaDto dto)
        {
            var r = await _servicio.ActualizarAsync(id, dto);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "ADMIN")]
        public async Task<ActionResult<ResultadoOperacion<bool>>> Desactivar(int id)
        {
            var r = await _servicio.DesactivarAsync(id);
            return r.Exito ? Ok(r) : BadRequest(r);
        }
    }
}
