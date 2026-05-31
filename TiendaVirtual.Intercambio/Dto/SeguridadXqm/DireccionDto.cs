using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiendaVirtual.Intercambio.Dto.SeguridadXqm
{
    public class DireccionDto
    {
        public int DireccionId { get; set; }
        public int PersonaId { get; set; }
        public string? Etiqueta { get; set; }
        public string NombreReceptor { get; set; } = null!;
        public string? Telefono { get; set; }
        public string Departamento { get; set; } = null!;
        public string Provincia { get; set; } = null!;
        public string Distrito { get; set; } = null!;
        public string DireccionLinea { get; set; } = null!;
        public string? Referencia { get; set; }
        public bool EsPredeterminada { get; set; }
    }
}
