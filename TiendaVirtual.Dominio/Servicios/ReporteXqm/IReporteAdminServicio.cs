using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.ReporteXqm;

namespace TiendaVirtual.Dominio.Servicios.ReporteXqm
{
    public interface IReporteAdminServicio
    {
        Task<ResultadoOperacion<ReporteAdminTotalesDto>> ObtenerTotalesDashboardAsync();
        Task<ResultadoOperacion<ReporteAdminDashboardDto>> ObtenerDashboardAsync(int? anio = null, int dias = 30);
    }
}
