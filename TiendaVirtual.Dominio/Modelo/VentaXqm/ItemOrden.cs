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

        /// <summary>Precio final cobrado por unidad (después del descuento si aplicó).</summary>
        public decimal PrecioUnitario { get; set; }

        /// <summary>
        /// Precio original por unidad ANTES del descuento. Se guarda solo cuando
        /// hubo oferta aplicada; queda en <c>null</c> si el item se cobró a precio
        /// pleno. Permite mostrar el precio tachado y el ahorro real en el
        /// histórico de pedidos (comprador y vendedor) sin depender de la oferta
        /// actual (que podría haber terminado o cambiado).
        /// </summary>
        public decimal? PrecioOriginal { get; set; }

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
