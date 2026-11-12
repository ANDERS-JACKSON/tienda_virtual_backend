using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Dominio.Modelo.VentaXqm;

namespace TiendaVirtual.Dominio.Modelo.CatalogoXqm
{
    public class VarianteProducto
    {
        public int VarianteId { get; set; }

        [Required]
        public int ProductoId { get; set; }

        public string? Sku { get; set; }
        public string? Nombre { get; set; }
        public decimal Precio { get; set; }
        public int? PesoGramos { get; set; }
        public string? Atributos { get; set; }   // JSONB
        public bool Activa { get; set; }

        public int? OpcionVarianteId { get; set; }

        [MaxLength(200)]
        public string? Material { get; set; }

        [MaxLength(200)]
        public string? Dimensiones { get; set; }

        [MaxLength(150)]
        public string? DescripcionCorta { get; set; }

        public virtual Producto Producto { get; set; } = null!;
        public virtual OpcionVariante? OpcionVariante { get; set; }
        public virtual Stock? Stock { get; set; }
        public virtual ICollection<ItemCarrito> ItemsCarrito { get; set; } = new List<ItemCarrito>();
        public virtual ICollection<ItemOrden> ItemsOrden { get; set; } = new List<ItemOrden>();
    }
}
