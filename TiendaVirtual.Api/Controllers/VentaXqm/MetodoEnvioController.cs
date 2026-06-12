using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TiendaVirtual.Dominio.Servicios.VentaXqm;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.VentaXqm;

namespace TiendaVirtual.Api.Controllers.VentaXqm
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class MetodoEnvioController : ControllerBase
    {
        private readonly IMetodoEnvioServicio _servicio;

        public MetodoEnvioController(IMetodoEnvioServicio servicio)
        {
            _servicio = servicio;
        }

        [HttpGet]
        public async Task<ActionResult<ResultadoOperacion<List<MetodoEnvioDto>>>> Listar()
        {
            var r = await _servicio.ListarActivosAsync();
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpGet("admin")]
        [Authorize(Roles = "ADMIN")]
        public async Task<ActionResult<ResultadoOperacion<List<MetodoEnvioAdminDto>>>> ListarAdmin()
        {
            var r = await _servicio.ListarTodosAdminAsync();
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpPost]
        [Authorize(Roles = "ADMIN")]
        public async Task<ActionResult<ResultadoOperacion<MetodoEnvioAdminDto>>> Crear([FromBody] CrearMetodoEnvioDto dto)
        {
            var r = await _servicio.CrearAsync(dto);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "ADMIN")]
        public async Task<ActionResult<ResultadoOperacion<MetodoEnvioAdminDto>>> Actualizar(
            int id, [FromBody] ActualizarMetodoEnvioDto dto)
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
    }
}
