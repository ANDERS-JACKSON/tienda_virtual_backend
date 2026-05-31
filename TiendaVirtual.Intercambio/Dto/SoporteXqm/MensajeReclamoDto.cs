using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiendaVirtual.Intercambio.Dto.SoporteXqm
{
    public class MensajeReclamoDto
    {
        public long MensajeId { get; set; }
        public long ReclamoId { get; set; }
        public int RemitenteId { get; set; }
        public string Mensaje { get; set; } = null!;
        public string? Adjuntos { get; set; }
        public DateTime Fecha { get; set; }
    }
}
