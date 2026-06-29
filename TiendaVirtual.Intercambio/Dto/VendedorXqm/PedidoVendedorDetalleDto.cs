using TiendaVirtual.Intercambio.Dto.VentaXqm;

namespace TiendaVirtual.Intercambio.Dto.VendedorXqm
{
    public class PedidoVendedorDetalleDto : PedidoVendedorDto
    {
        public string NumeroOrden { get; set; } = null!;
        public decimal MontoComision { get; set; }
        public DateTime? FechaEnvio { get; set; }
        public DateTime? FechaEntrega { get; set; }
        public string? TelefonoCliente { get; set; }
        public string? MetodoEnvioCodigo { get; set; }
        public string? MetodoEnvioDescripcion { get; set; }
        public int? MetodoEnvioTiempoEstimadoDias { get; set; }
        public DireccionSnapshotDto DireccionEnvio { get; set; } = new();
        public EnvioDto? Envio { get; set; }
        public List<ItemOrdenDto> Items { get; set; } = new();
    }
}
