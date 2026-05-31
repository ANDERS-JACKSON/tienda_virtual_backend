using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Intercambio.Dto.Sistema;

namespace TiendaVirtual.Intercambio.Dto.SeguridadXqm
{
    public class UsuarioDto
    {
        public int UsuarioId { get; set; }
        public int PersonaId { get; set; }
        public string Correo { get; set; } = null!;
        public string Contrasena { get; set; } = null!;
        public bool CorreoConfirmado { get; set; }
        public bool ForzarCambioClave { get; set; }
        public EnumeracionDto Estado { get; set; } = null!;
        public DateTime FechaAlta { get; set; }
        public DateTime? UltimoAcceso { get; set; }
    }
}
