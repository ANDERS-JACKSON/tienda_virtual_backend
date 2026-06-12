using TiendaVirtual.Intercambio.Dto.Sistema;

namespace TiendaVirtual.Intercambio.Dto.VentaXqm
{
    public class OrdenAdminListadoDto
    {
        public long OrdenId { get; set; }
        public string NumeroOrden { get; set; } = null!;
        public string CorreoCliente { get; set; } = null!;
        public string NombreCliente { get; set; } = null!;
        public decimal Total { get; set; }
        public EnumeracionDto Estado { get; set; } = null!;
        public int CantidadSubordenes { get; set; }
        public DateTime FechaCreacion { get; set; }
    }

    public class OrdenAdminResumenDto
    {
        public int TotalOrdenesHoy { get; set; }
        public int TotalOrdenesMes { get; set; }
        public int PendientesPago { get; set; }
    }

    public class CancelarOrdenAdminDto
    {
        public string Motivo { get; set; } = null!;
    }
}
