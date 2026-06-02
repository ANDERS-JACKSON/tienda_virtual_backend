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
        public decimal Subtotal { get; set; }
        public bool TieneItemsSinStock { get; set; }
    }
}
