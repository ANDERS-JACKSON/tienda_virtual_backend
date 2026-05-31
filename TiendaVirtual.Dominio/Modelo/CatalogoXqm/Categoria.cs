using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiendaVirtual.Dominio.Modelo.CatalogoXqm
{
    public class Categoria
    {
        public int CategoriaId { get; set; }
        public int? CategoriaPadreId { get; set; }

        [Required]
        public string Nombre { get; set; } = null!;

        [Required]
        public string Slug { get; set; } = null!;

        public string? Descripcion { get; set; }
        public string? ImagenUrl { get; set; }
        public int Orden { get; set; }
        public bool Activa { get; set; }

        public virtual Categoria? CategoriaPadre { get; set; }
        public virtual ICollection<Categoria> Subcategorias { get; set; } = new List<Categoria>();
        public virtual ICollection<Producto> Productos { get; set; } = new List<Producto>();
    }
}
