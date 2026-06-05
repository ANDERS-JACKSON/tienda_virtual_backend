using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TiendaVirtual.Dominio.Servicios.SuscripcionXqm;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.VendedorXqm;

namespace TiendaVirtual.Api.Controllers.SuscripcionXqm
{
    [ApiController]
    [Route("api/[controller]")]
    public class PlanController : ControllerBase
    {
        private readonly IPlanServicio _servicio;

        public PlanController(IPlanServicio servicio) => _servicio = servicio;

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<ResultadoOperacion<List<PlanDto>>>> Listar()
        {
            var r = await _servicio.ListarActivosAsync();
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpGet("admin")]
        [Authorize(Roles = "ADMIN")]
        public async Task<ActionResult<ResultadoOperacion<List<PlanDto>>>> ListarTodos()
        {
            var r = await _servicio.ListarTodosAsync();
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<ActionResult<ResultadoOperacion<PlanDto>>> Obtener(int id)
        {
            var r = await _servicio.ObtenerPorIdAsync(id);
            return r.Exito ? Ok(r) : NotFound(r);
        }

        [HttpPost]
        [Authorize(Roles = "ADMIN")]
        public async Task<ActionResult<ResultadoOperacion<PlanDto>>> Crear([FromBody] CrearPlanDto dto)
        {
            var r = await _servicio.CrearAsync(dto);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "ADMIN")]
        public async Task<ActionResult<ResultadoOperacion<PlanDto>>> Actualizar(int id, [FromBody] ActualizarPlanDto dto)
        {
            var r = await _servicio.ActualizarAsync(id, dto);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpPost("{id:int}/activar")]
        [Authorize(Roles = "ADMIN")]
        public async Task<ActionResult<ResultadoOperacion<bool>>> Activar(int id)
        {
            var r = await _servicio.CambiarEstadoAsync(id, true);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpPost("{id:int}/desactivar")]
        [Authorize(Roles = "ADMIN")]
        public async Task<ActionResult<ResultadoOperacion<bool>>> Desactivar(int id)
        {
            var r = await _servicio.CambiarEstadoAsync(id, false);
            return r.Exito ? Ok(r) : BadRequest(r);
        }
    }
}
