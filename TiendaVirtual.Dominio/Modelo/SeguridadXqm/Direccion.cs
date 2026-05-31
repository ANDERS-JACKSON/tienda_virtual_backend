using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiendaVirtual.Dominio.Modelo.SeguridadXqm
{
    public class Direccion
    {
        public int DireccionId { get; set; }

        [Required]
        public int PersonaId { get; set; }

        public string? Etiqueta { get; set; }

        [Required]
        public string NombreReceptor { get; set; } = null!;

        public string? Telefono { get; set; }

        [Required]
        public string Departamento { get; set; } = null!;

        [Required]
        public string Provincia { get; set; } = null!;

        [Required]
        public string Distrito { get; set; } = null!;

        [Required]
        public string DireccionLinea { get; set; } = null!;

        public string? Referencia { get; set; }
        public bool EsPredeterminada { get; set; }

        public virtual Persona Persona { get; set; } = null!;
    }
}
