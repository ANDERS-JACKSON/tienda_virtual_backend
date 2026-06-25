using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TiendaVirtual.Dominio.Servicios.ConfiguracionXqm;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.ConfiguracionXqm;

namespace TiendaVirtual.Api.Controllers.ConfiguracionXqm
{
    [ApiController]
    [Route("api/[controller]")]
    public class AvisoBannerController : ControllerBase
    {
        private readonly IAvisoBannerServicio _servicio;

        public AvisoBannerController(IAvisoBannerServicio servicio) => _servicio = servicio;

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<ResultadoOperacion<List<AvisoBannerDto>>>> Listar()
        {
            var r = await _servicio.ListarActivosAsync();
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpGet("admin")]
        [Authorize(Roles = "ADMIN")]
        public async Task<ActionResult<ResultadoOperacion<List<AvisoBannerAdminDto>>>> ListarAdmin()
        {
            var r = await _servicio.ListarTodosAdminAsync();
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpPost]
        [Authorize(Roles = "ADMIN")]
        public async Task<ActionResult<ResultadoOperacion<AvisoBannerAdminDto>>> Crear(
            [FromBody] CrearAvisoBannerDto dto)
        {
            var r = await _servicio.CrearAsync(dto);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "ADMIN")]
        public async Task<ActionResult<ResultadoOperacion<AvisoBannerAdminDto>>> Actualizar(
            int id, [FromBody] ActualizarAvisoBannerDto dto)
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
