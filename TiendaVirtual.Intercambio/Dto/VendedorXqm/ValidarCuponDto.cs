using System.ComponentModel.DataAnnotations;

namespace TiendaVirtual.Intercambio.Dto.VendedorXqm
{
    public class ValidarCuponDto
    {
        [Required, MaxLength(50)]
        public string Codigo { get; set; } = null!;

        public int? PlanId { get; set; }
    }
}
