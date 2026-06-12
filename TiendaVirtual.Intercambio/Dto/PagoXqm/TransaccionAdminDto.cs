using TiendaVirtual.Intercambio.Dto.Sistema;

namespace TiendaVirtual.Intercambio.Dto.PagoXqm
{
    public class TransaccionAdminListadoDto
    {
        public long TransaccionId { get; set; }
        public string? TransaccionProveedorId { get; set; }
        public EnumeracionDto Tipo { get; set; } = null!;
        public decimal Monto { get; set; }
        public EnumeracionDto Estado { get; set; } = null!;
        public string? MetodoPago { get; set; }
        public DateTime Fecha { get; set; }
    }

    public class TransaccionAdminDetalleDto : TransaccionAdminListadoDto
    {
        public string Proveedor { get; set; } = null!;
        public long? OrdenId { get; set; }
        public int? SuscripcionId { get; set; }
        public int UsuarioId { get; set; }
        public string? RespuestaProveedor { get; set; }
    }

    public class TransaccionAdminResumenDto
    {
        public decimal IngresosHoy { get; set; }
        public decimal IngresosMes { get; set; }
        public decimal TotalPendiente { get; set; }
    }

    public class MarcarTransaccionFallidaDto
    {
        public string Motivo { get; set; } = null!;
    }
}
