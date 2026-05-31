using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Intercambio.Dto.Sistema;

namespace TiendaVirtual.Intercambio.Dto.VentaXqm
{
    public class ItemOrdenDto
    {
        public long ItemOrdenId { get; set; }
        public long SubordenId { get; set; }
        public int? VarianteId { get; set; }
        public string NombreProducto { get; set; } = null!;
        public string? NombreVariante { get; set; }
        public decimal PrecioUnitario { get; set; }
        public int Cantidad { get; set; }
        public decimal TotalLinea { get; set; }
        public string? ImagenUrl { get; set; }
        public EnumeracionDto TipoProducto { get; set; } = null!;
        public string? ArchivoPatronUrl { get; set; }
    }
}
