using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiendaVirtual.Intercambio.Dto.SeguridadXqm
{
    public class RegistroVendedorDto
    {
        [Required]
        [EmailAddress]
        public string Correo { get; set; } = null!;

        [Required]
        [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
        public string Contrasena { get; set; } = null!;

        [Required]
        public PersonaDto Persona { get; set; } = null!;

        [Required]
        [MaxLength(150)]
        public string NombreTienda { get; set; } = null!;

        [Required]
        [MaxLength(150)]
        [RegularExpression(@"^[a-z0-9\-]+$",
            ErrorMessage = "El slug solo puede contener letras minúsculas, números y guiones.")]
        public string SlugTienda { get; set; } = null!;

        [MaxLength(1000)]
        public string? Biografia { get; set; }

        [MaxLength(20)]
        public string? NumeroYape { get; set; }
    }
}
