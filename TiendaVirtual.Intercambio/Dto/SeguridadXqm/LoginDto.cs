using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiendaVirtual.Intercambio.Dto.SeguridadXqm
{
    public class LoginDto
    {
        [Required]
        [EmailAddress]
        public string Correo { get; set; } = null!;

        [Required]
        [MinLength(8)]
        public string Contrasena { get; set; } = null!;
    }
}
