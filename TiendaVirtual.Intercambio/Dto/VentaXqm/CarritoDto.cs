using System.Collections.Generic;

namespace TiendaVirtual.Intercambio.Dto.VentaXqm
{
    public class GrupoVendedorCarritoDto
    {
        public int VendedorId { get; set; }
        public string NombreTienda { get; set; } = null!;
        public string SlugTienda { get; set; } = null!;
        public List<ItemCarritoDto> Items { get; set; } = new();
        public decimal SubtotalGrupo { get; set; }
    }

    public class CarritoDto
    {
        public List<GrupoVendedorCarritoDto> Vendedores { get; set; } = new();
        public int TotalItems { get; set; }
        /// <summary>Suma de líneas (ya con ofertas de producto).</summary>
        public decimal Subtotal { get; set; }
        /// <summary>Descuento del cupón de pedido aplicado.</summary>
        public decimal DescuentoCupon { get; set; }
        /// <summary>Subtotal − descuento cupón.</summary>
        public decimal Total { get; set; }
        public bool TieneItemsSinStock { get; set; }
        public CuponPedidoAplicadoDto? Cupon { get; set; }
    }
}
