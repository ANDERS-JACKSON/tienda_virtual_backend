using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiendaVirtual.Intercambio.Dto.CatalogoXqm
{
    public class FavoritoDto
    {
        public int UsuarioId { get; set; }
        public int ProductoId { get; set; }
        public DateTime Fecha { get; set; }
        public ProductoListadoDto Producto { get; set; } = null!;
    }
}
