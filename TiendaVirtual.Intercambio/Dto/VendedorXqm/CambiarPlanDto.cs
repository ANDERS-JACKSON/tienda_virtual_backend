using System.ComponentModel.DataAnnotations;

namespace TiendaVirtual.Intercambio.Dto.VendedorXqm
{
    public class CambiarPlanDto
    {
        [Required]
        public int NuevoPlanId { get; set; }
    }
}
