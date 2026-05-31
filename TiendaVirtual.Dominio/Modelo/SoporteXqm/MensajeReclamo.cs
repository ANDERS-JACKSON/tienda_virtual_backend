using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Dominio.Modelo.SeguridadXqm;

namespace TiendaVirtual.Dominio.Modelo.SoporteXqm
{
    public class MensajeReclamo
    {
        public long MensajeId { get; set; }

        [Required]
        public long ReclamoId { get; set; }

        [Required]
        public int RemitenteId { get; set; }

        [Required]
        public string Mensaje { get; set; } = null!;

        public string? Adjuntos { get; set; }   // JSONB
        public DateTime Fecha { get; set; }

        public virtual Reclamo Reclamo { get; set; } = null!;
        public virtual Usuario Remitente { get; set; } = null!;
    }
}
