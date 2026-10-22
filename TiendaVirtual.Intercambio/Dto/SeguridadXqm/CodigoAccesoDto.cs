using System.ComponentModel.DataAnnotations;

namespace TiendaVirtual.Intercambio.Dto.SeguridadXqm
{
    public class SolicitarCodigoAccesoDto
    {
        [Required]
        [EmailAddress]
        [MaxLength(254)]
        public string Correo { get; set; } = null!;
    }

    public class VerificarCodigoAccesoDto
    {
        [Required]
        [EmailAddress]
        [MaxLength(254)]
        public string Correo { get; set; } = null!;

        [Required]
        [RegularExpression(@"^\d{4}$", ErrorMessage = "El código debe tener 4 dígitos.")]
        public string Codigo { get; set; } = null!;
    }

    public class CompletarRegistroCodigoDto
    {
        [Required]
        public string Ticket { get; set; } = null!;

        [Required]
        public PersonaDto Persona { get; set; } = null!;

        public bool AceptaTerminos { get; set; }
    }

    public class PrepararCompraInvitadoDto
    {
        [Required]
        [EmailAddress]
        [MaxLength(254)]
        public string Correo { get; set; } = null!;

        [Required]
        public PersonaDto Persona { get; set; } = null!;

        public bool AceptaTerminos { get; set; }
    }
}
