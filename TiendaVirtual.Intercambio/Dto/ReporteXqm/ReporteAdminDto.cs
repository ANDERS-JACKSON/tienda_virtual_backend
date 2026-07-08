using TiendaVirtual.Intercambio.Dto.Sistema;

namespace TiendaVirtual.Intercambio.Dto.ReporteXqm
{
    public class ReporteAdminDashboardDto
    {
        public int Anio { get; set; }
        public ReporteAdminResumenDto Resumen { get; set; } = new();
        public List<ReporteSerieTemporalDto> IngresosMensuales { get; set; } = new();
        public List<ReporteSerieTemporalDto> OrdenesDiarias { get; set; } = new();
        public List<ReporteProductoTopDto> ProductosMasVendidos { get; set; } = new();
        public List<ReporteClienteTopDto> ClientesTop { get; set; } = new();
        public List<ReporteVendedorTopDto> VendedoresTop { get; set; } = new();
        public List<ReporteCategoriaDto> VentasPorCategoria { get; set; } = new();
        public List<ReporteEstadoDto> OrdenesPorEstado { get; set; } = new();
    }

    public class ReporteAdminResumenDto
    {
        public decimal IngresosMes { get; set; }
        public decimal IngresosHoy { get; set; }
        public int OrdenesMes { get; set; }
        public int OrdenesHoy { get; set; }
        public int VendedoresActivos { get; set; }
        public int ProductosPublicados { get; set; }
        public int ClientesConCompras { get; set; }
        public int VerificacionesPendientes { get; set; }
        public decimal TicketPromedioMes { get; set; }
        public decimal ComisionPlataformaMes { get; set; }
    }

    /// <summary>KPIs ligeros para el dashboard admin (sin gráficos ni rankings).</summary>
    public class ReporteAdminTotalesDto
    {
        public int VerificacionesPendientes { get; set; }
        public int VendedoresActivos { get; set; }
        public int ProductosPublicados { get; set; }
        public int OrdenesMes { get; set; }
        public decimal IngresosMes { get; set; }
    }

    public class ReporteSerieTemporalDto
    {
        public string Etiqueta { get; set; } = null!;
        public DateTime Fecha { get; set; }
        public decimal Monto { get; set; }
        public int Cantidad { get; set; }
    }

    public class ReporteProductoTopDto
    {
        public string NombreProducto { get; set; } = null!;
        public int UnidadesVendidas { get; set; }
        public decimal Ingresos { get; set; }
        public string? ImagenUrl { get; set; }
    }

    public class ReporteClienteTopDto
    {
        public long ClienteId { get; set; }
        public string NombreCliente { get; set; } = null!;
        public string Correo { get; set; } = null!;
        public int TotalOrdenes { get; set; }
        public decimal GastoTotal { get; set; }
    }

    public class ReporteVendedorTopDto
    {
        public int VendedorId { get; set; }
        public string NombreTienda { get; set; } = null!;
        public int VentasEntregadas { get; set; }
        public decimal IngresosGenerados { get; set; }
        public decimal ComisionPlataforma { get; set; }
    }

    public class ReporteCategoriaDto
    {
        public int CategoriaId { get; set; }
        public string NombreCategoria { get; set; } = null!;
        public int UnidadesVendidas { get; set; }
        public decimal Ingresos { get; set; }
    }

    public class ReporteEstadoDto
    {
        public EnumeracionDto Estado { get; set; } = null!;
        public int Cantidad { get; set; }
        public decimal MontoTotal { get; set; }
    }
}
