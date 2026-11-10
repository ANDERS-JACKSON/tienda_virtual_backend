using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TiendaVirtual.Dominio.Servicios.ConfiguracionXqm;
using TiendaVirtual.Intercambio.Dto.ConfiguracionXqm;

namespace TiendaVirtual.Api.Controllers.ConfiguracionXqm
{
    [ApiController]
    [Route("api/[controller]")]
    public class FranjaInicioController : ControllerBase
    {
        private readonly IFranjaInicioServicio _servicio;

        public FranjaInicioController(IFranjaInicioServicio servicio)
        {
            _servicio = servicio;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ListarActivos([FromQuery] string zona)
        {
            var r = await _servicio.ListarActivosAsync(zona);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpGet("admin")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> ListarAdmin([FromQuery] string? zona)
        {
            var r = await _servicio.ListarTodosAdminAsync(zona);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpPost]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> Crear([FromBody] CrearFranjaInicioDto dto)
        {
            var r = await _servicio.CrearAsync(dto);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> Actualizar(int id, [FromBody] ActualizarFranjaInicioDto dto)
        {
            var r = await _servicio.ActualizarAsync(id, dto);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> Eliminar(int id)
        {
            var r = await _servicio.EliminarAsync(id);
            return r.Exito ? Ok(r) : BadRequest(r);
        }
    }
}
