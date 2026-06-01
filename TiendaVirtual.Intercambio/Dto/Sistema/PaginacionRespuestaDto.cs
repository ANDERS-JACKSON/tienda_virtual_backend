using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiendaVirtual.Intercambio.Dto.Sistema
{
    public class PaginacionRespuestaDto<T>
    {
        public List<T> Items { get; set; } = new();
        public int Pagina { get; set; }
        public int TamanioPagina { get; set; }
        public int TotalRegistros { get; set; }
        public bool HayMas { get; set; }
    }
}
