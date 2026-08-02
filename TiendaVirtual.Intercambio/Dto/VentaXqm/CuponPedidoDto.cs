using TiendaVirtual.Intercambio.Dto.Sistema;

namespace TiendaVirtual.Intercambio.Dto.VentaXqm
{
    public class CuponPedidoDto
    {
        public int CuponPedidoId { get; set; }
        public string Codigo { get; set; } = null!;
        public EnumeracionDto TipoDescuento { get; set; } = null!;
        public decimal ValorDescuento { get; set; }
        public decimal? MontoMinimo { get; set; }
        public int? UsosMaximos { get; set; }
        public int UsosRealizados { get; set; }
        public DateTime? ValidoHasta { get; set; }
        public bool Activo { get; set; }
        public string? Descripcion { get; set; }
        public DateTime FechaCreacion { get; set; }
    }

    public class CrearCuponPedidoDto
    {
        public string Codigo { get; set; } = null!;
        public EnumeracionDto TipoDescuento { get; set; } = null!;
        public decimal ValorDescuento { get; set; }
        public decimal? MontoMinimo { get; set; }
        public int? UsosMaximos { get; set; }
        public DateTime? ValidoHasta { get; set; }
        public string? Descripcion { get; set; }
    }

    public class ActualizarCuponPedidoDto
    {
        public decimal ValorDescuento { get; set; }
        public decimal? MontoMinimo { get; set; }
        public int? UsosMaximos { get; set; }
        public DateTime? ValidoHasta { get; set; }
        public bool Activo { get; set; }
        public string? Descripcion { get; set; }
    }
}
