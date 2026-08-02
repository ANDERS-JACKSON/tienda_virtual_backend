using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TiendaVirtual.Dominio.Servicios.VentaXqm;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.VentaXqm;

namespace TiendaVirtual.Api.Controllers.VentaXqm
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "ADMIN")]
    public class CuponPedidoController : ControllerBase
    {
        private readonly ICuponPedidoServicio _servicio;

        public CuponPedidoController(ICuponPedidoServicio servicio) => _servicio = servicio;

        [HttpGet]
        public async Task<ActionResult<ResultadoOperacion<List<CuponPedidoDto>>>> Listar()
        {
            var r = await _servicio.ListarAsync();
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpPost]
        public async Task<ActionResult<ResultadoOperacion<CuponPedidoDto>>> Crear(
            [FromBody] CrearCuponPedidoDto dto)
        {
            var r = await _servicio.CrearAsync(dto);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<ResultadoOperacion<CuponPedidoDto>>> Actualizar(
            int id, [FromBody] ActualizarCuponPedidoDto dto)
        {
            var r = await _servicio.ActualizarAsync(id, dto);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpPost("{id:int}/activar")]
        public async Task<ActionResult<ResultadoOperacion<bool>>> Activar(int id)
        {
            var r = await _servicio.ActivarAsync(id);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpPost("{id:int}/desactivar")]
        public async Task<ActionResult<ResultadoOperacion<bool>>> Desactivar(int id)
        {
            var r = await _servicio.DesactivarAsync(id);
            return r.Exito ? Ok(r) : BadRequest(r);
        }
    }
}
