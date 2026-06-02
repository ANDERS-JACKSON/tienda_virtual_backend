using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiendaVirtual.Intercambio.Dto.CatalogoXqm
{
    public class CategoriaArbolDto
    {
        public int CategoriaId { get; set; }
        public string Nombre { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string? ImagenUrl { get; set; }
        public int Orden { get; set; }
        public int TotalProductos { get; set; }
        public List<CategoriaArbolDto> Subcategorias { get; set; } = new();
    }
}
