using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiendaVirtual.Intercambio.Dto.CatalogoXqm
{
    public class ImagenProductoDto
    {
        public int ImagenId { get; set; }
        public int ProductoId { get; set; }
        public string Url { get; set; } = null!;
        public string? TextoAlt { get; set; }
        public int Orden { get; set; }
        public bool EsPrincipal { get; set; }
    }
}
