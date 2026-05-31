using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiendaVirtual.Intercambio.Dto.CatalogoXqm
{
    public class CategoriaDto
    {
        public int CategoriaId { get; set; }
        public int? CategoriaPadreId { get; set; }
        public string Nombre { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string? Descripcion { get; set; }
        public string? ImagenUrl { get; set; }
        public int Orden { get; set; }
        public bool Activa { get; set; }
    }
}
