using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiendaVirtual.Intercambio.Dto.SeguridadXqm
{
    public class Verificar2FaRequestDto
    {
        [Required]
        public string TokenTemporal { get; set; } = null!;

        [Required]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "El código debe tener 6 dígitos.")]
        public string Codigo { get; set; } = null!;
    }
}
