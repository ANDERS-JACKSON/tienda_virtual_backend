using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiendaVirtual.Intercambio.Dto.SoporteXqm
{
    public class NotificacionDto
    {
        public long NotificacionId { get; set; }
        public int UsuarioId { get; set; }
        public string Tipo { get; set; } = null!;
        public string Titulo { get; set; } = null!;
        public string? Cuerpo { get; set; }
        public string? Datos { get; set; }
        public bool Leida { get; set; }
        public DateTime Fecha { get; set; }
    }
}
