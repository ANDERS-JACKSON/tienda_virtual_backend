using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Servicios.PagoXqm;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.PagoXqm;
using TiendaVirtual.Intercambio.Dto.Sistema;

namespace TiendaVirtual.Api.Controllers.PagoXqm
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "ADMIN")]
    public class TransaccionController : ControllerBase
    {
        private readonly ITransaccionAdminServicio _servicio;

        public TransaccionController(ITransaccionAdminServicio servicio) => _servicio = servicio;

        [HttpGet("admin")]
        public async Task<ActionResult<ResultadoOperacion<PaginacionRespuestaDto<TransaccionAdminListadoDto>>>> Listar(
            [FromQuery] TipoTransaccion? tipo = null,
            [FromQuery] TipoEstadoTransaccion? estado = null,
            [FromQuery] DateTime? fechaDesde = null,
            [FromQuery] DateTime? fechaHasta = null,
            [FromQuery] int pagina = 1,
            [FromQuery] int tamanioPagina = 20)
        {
            var r = await _servicio.ListarAsync(tipo, estado, fechaDesde, fechaHasta, pagina, tamanioPagina);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpGet("admin/resumen")]
        public async Task<ActionResult<ResultadoOperacion<TransaccionAdminResumenDto>>> Resumen()
        {
            var r = await _servicio.ObtenerResumenAsync();
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpGet("admin/{id:long}")]
        public async Task<ActionResult<ResultadoOperacion<TransaccionAdminDetalleDto>>> Detalle(long id)
        {
            var r = await _servicio.ObtenerDetalleAsync(id);
            return r.Exito ? Ok(r) : NotFound(r);
        }

        [HttpPost("admin/{id:long}/marcar-completada")]
        public async Task<ActionResult<ResultadoOperacion<bool>>> MarcarCompletada(long id)
        {
            var r = await _servicio.MarcarCompletadaAsync(id);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpPost("admin/{id:long}/marcar-fallida")]
        public async Task<ActionResult<ResultadoOperacion<bool>>> MarcarFallida(
            long id, [FromBody] MarcarTransaccionFallidaDto dto)
        {
            var r = await _servicio.MarcarFallidaAsync(id, dto);
            return r.Exito ? Ok(r) : BadRequest(r);
        }
    }
}
