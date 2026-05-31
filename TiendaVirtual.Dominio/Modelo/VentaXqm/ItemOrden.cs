using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Modelo.CatalogoXqm;
using TiendaVirtual.Dominio.Modelo.SoporteXqm;

namespace TiendaVirtual.Dominio.Modelo.VentaXqm
{
    public class ItemOrden
    {
        public long ItemOrdenId { get; set; }

        [Required]
        public long SubordenId { get; set; }

        public int? VarianteId { get; set; }

        [Required]
        public string NombreProducto { get; set; } = null!;

        public string? NombreVariante { get; set; }
        public decimal PrecioUnitario { get; set; }
        public int Cantidad { get; set; }
        public decimal TotalLinea { get; set; }
        public string? ImagenUrl { get; set; }
        public TipoProducto TipoProducto { get; set; }
        public string? ArchivoPatronUrl { get; set; }

        public virtual Suborden Suborden { get; set; } = null!;
        public virtual VarianteProducto? Variante { get; set; }
        public virtual ResenaProducto? Resena { get; set; }
    }
}
