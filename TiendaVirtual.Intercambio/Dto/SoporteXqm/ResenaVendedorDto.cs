using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiendaVirtual.Intercambio.Dto.SoporteXqm
{
    public class ResenaVendedorDto
    {
        public long ResenaId { get; set; }
        public int VendedorId { get; set; }
        public long SubordenId { get; set; }
        public int ClienteId { get; set; }
        public short Calificacion { get; set; }
        public string? Comentario { get; set; }
        public DateTime Fecha { get; set; }
    }
}
