using System;
using System.Collections.Generic;
using TiendaVirtual.Intercambio.Dto.Sistema;

namespace TiendaVirtual.Intercambio.Dto.VentaXqm
{
    public class SubordenDto
    {
        public long SubordenId { get; set; }
        public long OrdenId { get; set; }
        public int VendedorId { get; set; }
        public string NombreTienda { get; set; } = string.Empty;
        public string SlugTienda { get; set; } = string.Empty;
        public string NumeroSuborden { get; set; } = null!;
        public string? MetodoEnvio { get; set; }
        public decimal Subtotal { get; set; }
        public decimal MontoEnvio { get; set; }
        public decimal MontoComision { get; set; }
        public decimal MontoVendedor { get; set; }
        public EnumeracionDto Estado { get; set; } = null!;
        public DateTime? FechaEnvio { get; set; }
        public DateTime? FechaEntrega { get; set; }
        public string? CodigoSeguimiento { get; set; }
        public string? CodigoOrdenAgencia { get; set; }
        public string? ClaveRecojo { get; set; }
        public string? DetallesEnvio { get; set; }
        public string? TransportistaEnvio { get; set; }
        public string? ComprobanteEnvioUrl { get; set; }
        public List<ItemOrdenDto> Items { get; set; } = new();
    }
}
