using System.ComponentModel.DataAnnotations;

namespace TiendaVirtual.Intercambio.Dto.VendedorXqm
{
    public class IniciarSuscripcionDto
    {
        [Required]
        public int PlanId { get; set; }

        [MaxLength(50)]
        public string? CodigoCupon { get; set; }
    }
}
