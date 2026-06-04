using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiendaVirtual.Intercambio.Dto.SeguridadXqm
{
    public class RegistroClienteDto
    {
        [Required]
        [EmailAddress]
        public string Correo { get; set; } = null!;

        [Required]
        public PersonaDto Persona { get; set; } = null!;
    }
}
