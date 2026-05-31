using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Dominio.Modelo.SeguridadXqm;

namespace TiendaVirtual.Dominio.Modelo.SoporteXqm
{
    public class Notificacion
    {
        public long NotificacionId { get; set; }

        [Required]
        public int UsuarioId { get; set; }

        [Required]
        public string Tipo { get; set; } = null!;

        [Required]
        public string Titulo { get; set; } = null!;

        public string? Cuerpo { get; set; }
        public string? Datos { get; set; }   // JSONB
        public bool Leida { get; set; }
        public DateTime Fecha { get; set; }

        public virtual Usuario Usuario { get; set; } = null!;
    }
}
