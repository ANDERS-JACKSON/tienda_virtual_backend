using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TiendaVirtual.Dominio.Servicios.ConfiguracionXqm;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.ConfiguracionXqm;

namespace TiendaVirtual.Api.Controllers.ConfiguracionXqm
{
    [ApiController]
    [Route("api/[controller]")]
    public class CarruselInicioController : ControllerBase
    {
        private readonly ICarruselInicioServicio _servicio;

        public CarruselInicioController(ICarruselInicioServicio servicio) => _servicio = servicio;

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<ResultadoOperacion<List<CarruselInicioDto>>>> Listar()
        {
            var r = await _servicio.ListarActivosAsync();
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpGet("admin")]
        [Authorize(Roles = "ADMIN")]
        public async Task<ActionResult<ResultadoOperacion<List<CarruselInicioAdminDto>>>> ListarAdmin()
        {
            var r = await _servicio.ListarTodosAdminAsync();
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpPost]
        [Authorize(Roles = "ADMIN")]
        public async Task<ActionResult<ResultadoOperacion<CarruselInicioAdminDto>>> Crear(
            [FromBody] CrearCarruselInicioDto dto)
        {
            var r = await _servicio.CrearAsync(dto);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "ADMIN")]
        public async Task<ActionResult<ResultadoOperacion<CarruselInicioAdminDto>>> Actualizar(
            int id, [FromBody] ActualizarCarruselInicioDto dto)
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
