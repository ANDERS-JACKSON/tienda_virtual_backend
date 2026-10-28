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

        /// <summary>
        /// Token del widget de Cloudflare Turnstile. Opcional: sólo se exige cuando
        /// Turnstile:Activo=true en el servidor (ver ITurnstileService).
        /// </summary>
        public string? TurnstileToken { get; set; }
    }
}
