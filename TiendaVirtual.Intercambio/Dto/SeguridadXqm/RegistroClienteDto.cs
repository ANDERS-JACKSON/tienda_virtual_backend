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

        /// <summary>Debe ser true. Un bool en false pasa el [Required] del modelo.</summary>
        public bool AceptaTerminos { get; set; }

        /// <summary>
        /// Campo honeypot anti-bot: debe permanecer vacío. En el formulario real está
        /// oculto para personas (CSS) y los bots que autocompletan campos lo llenan.
        /// No hay captcha visible aquí a pedido del negocio.
        /// </summary>
        public string? Sitio { get; set; }
    }
}
