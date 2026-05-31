using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Intercambio.Dto.Sistema;

namespace TiendaVirtual.Intercambio.Dto.SeguridadXqm
{
    public class PersonaDto
    {
        public int PersonaId { get; set; }
        public EnumeracionDto TipoDocumento { get; set; } = null!;
        public string NumeroDocumento { get; set; } = null!;
        public string? ApellidoPaterno { get; set; }
        public string? ApellidoMaterno { get; set; }
        public string Nombres { get; set; } = null!;
        public EnumeracionDto? Sexo { get; set; }
        public DateOnly? FechaNacimiento { get; set; }
        public string? Telefono { get; set; }
        public string? CorreoElectronico { get; set; }
    }
}
