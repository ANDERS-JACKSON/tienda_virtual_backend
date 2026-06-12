using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Servicios.VentaXqm;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.Sistema;
using TiendaVirtual.Intercambio.Dto.VentaXqm;

namespace TiendaVirtual.Api.Controllers.VentaXqm
{
    [ApiController]
    [Route("api/Orden")]
    [Authorize(Roles = "ADMIN")]
    public class OrdenAdminController : ControllerBase
    {
        private readonly IOrdenServicio _servicio;

        public OrdenAdminController(IOrdenServicio servicio) => _servicio = servicio;

        [HttpGet("admin")]
        public async Task<ActionResult<ResultadoOperacion<PaginacionRespuestaDto<OrdenAdminListadoDto>>>> Listar(
            [FromQuery] string? busqueda = null,
            [FromQuery] TipoEstadoOrden? estado = null,
            [FromQuery] DateTime? fechaDesde = null,
            [FromQuery] DateTime? fechaHasta = null,
            [FromQuery] int pagina = 1,
            [FromQuery] int tamanioPagina = 20)
        {
            var r = await _servicio.ListarAdminAsync(busqueda, estado, fechaDesde, fechaHasta, pagina, tamanioPagina);
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpGet("admin/resumen")]
        public async Task<ActionResult<ResultadoOperacion<OrdenAdminResumenDto>>> Resumen()
        {
            var r = await _servicio.ObtenerResumenAdminAsync();
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpGet("admin/{ordenId:long}")]
        public async Task<ActionResult<ResultadoOperacion<OrdenDto>>> Detalle(long ordenId)
        {
            var r = await _servicio.ObtenerAdminDetalleAsync(ordenId);
            return r.Exito ? Ok(r) : NotFound(r);
        }

        [HttpPost("admin/{ordenId:long}/cancelar")]
        public async Task<ActionResult<ResultadoOperacion<bool>>> Cancelar(
            long ordenId, [FromBody] CancelarOrdenAdminDto dto)
        {
            var r = await _servicio.CancelarAdminAsync(ordenId, dto);
            return r.Exito ? Ok(r) : BadRequest(r);
        }
    }
}
