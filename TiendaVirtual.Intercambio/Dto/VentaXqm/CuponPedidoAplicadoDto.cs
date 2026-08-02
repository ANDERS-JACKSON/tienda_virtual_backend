using TiendaVirtual.Intercambio.Dto.Sistema;

namespace TiendaVirtual.Intercambio.Dto.VentaXqm
{
    public class CuponPedidoAplicadoDto
    {
        public int CuponPedidoId { get; set; }
        public string Codigo { get; set; } = null!;
        public EnumeracionDto TipoDescuento { get; set; } = null!;
        public decimal ValorDescuento { get; set; }
        public decimal? MontoMinimo { get; set; }
        public string? Descripcion { get; set; }
        public decimal DescuentoAplicado { get; set; }
    }
}
