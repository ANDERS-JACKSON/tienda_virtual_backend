using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiendaVirtual.Dominio.Modelo.CatalogoXqm
{
    public class ImagenProducto
    {
        public int ImagenId { get; set; }

        [Required]
        public int ProductoId { get; set; }

        [Required]
        public string Url { get; set; } = null!;

        public string? TextoAlt { get; set; }
        public int Orden { get; set; }
        public bool EsPrincipal { get; set; }

        public virtual Producto Producto { get; set; } = null!;
    }
}
