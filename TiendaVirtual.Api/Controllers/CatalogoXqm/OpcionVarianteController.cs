using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TiendaVirtual.Dominio.Servicios.CatalogoXqm;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.CatalogoXqm;

namespace TiendaVirtual.Api.Controllers.CatalogoXqm
{
    [ApiController]
    [Route("api/[controller]")]
    public class OpcionVarianteController : ControllerBase
    {
        private readonly IOpcionVarianteServicio _servicio;

        public OpcionVarianteController(IOpcionVarianteServicio servicio) => _servicio = servicio;

        [HttpGet]
        [Authorize]
        public async Task<ActionResult<ResultadoOperacion<List<OpcionVarianteDto>>>> Listar()
        {
            var r = await _servicio.ListarActivasAsync();
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpGet("admin")]
        [Authorize(Roles = "ADMIN")]
        public async Task<ActionResult<ResultadoOperacion<List<OpcionVarianteDto>>>> ListarAdmin()
        {
            var r = await _servicio.ListarAdminAsync();
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpPost]
        [Authorize(Roles = "ADMIN")]
        public async Task<ActionResult<ResultadoOperacion<OpcionVarianteDto>>> Crear(
            [FromBody] CrearOpcionVarianteDto dto)
        {
            var r = await _servicio.CrearAsync(dto);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "ADMIN")]
        public async Task<ActionResult<ResultadoOperacion<OpcionVarianteDto>>> Actualizar(
            int id, [FromBody] ActualizarOpcionVarianteDto dto)
        {
            var r = await _servicio.ActualizarAsync(id, dto);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpPost("{id:int}/activar")]
        [Authorize(Roles = "ADMIN")]
        public async Task<ActionResult<ResultadoOperacion<bool>>> Activar(int id)
        {
            var r = await _servicio.ActivarAsync(id);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpPost("{id:int}/desactivar")]
        [Authorize(Roles = "ADMIN")]
        public async Task<ActionResult<ResultadoOperacion<bool>>> Desactivar(int id)
        {
            var r = await _servicio.DesactivarAsync(id);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "ADMIN")]
        public async Task<ActionResult<ResultadoOperacion<bool>>> Eliminar(int id)
        {
            var r = await _servicio.EliminarAsync(id);
            return r.Exito ? Ok(r) : BadRequest(r);
        }
    }
}
