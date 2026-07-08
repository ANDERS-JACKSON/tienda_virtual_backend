using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TiendaVirtual.Dominio.Servicios.ReporteXqm;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.ReporteXqm;

namespace TiendaVirtual.Api.Controllers.ReporteXqm
{
    [ApiController]
    [Route("api/Reporte")]
    [Authorize(Roles = "ADMIN")]
    public class ReporteAdminController : ControllerBase
    {
        private readonly IReporteAdminServicio _servicio;

        public ReporteAdminController(IReporteAdminServicio servicio) => _servicio = servicio;

        [HttpGet("admin/totales")]
        public async Task<ActionResult<ResultadoOperacion<ReporteAdminTotalesDto>>> Totales()
        {
            var r = await _servicio.ObtenerTotalesDashboardAsync();
            return r.Exito ? Ok(r) : BadRequest(r);
        }

        [HttpGet("admin/dashboard")]
        public async Task<ActionResult<ResultadoOperacion<ReporteAdminDashboardDto>>> Dashboard(
            [FromQuery] int? anio = null,
            [FromQuery] int dias = 30)
        {
            var r = await _servicio.ObtenerDashboardAsync(anio, dias);
            return r.Exito ? Ok(r) : BadRequest(r);
        }
    }
}
