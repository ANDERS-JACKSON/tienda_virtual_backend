using System.ComponentModel.DataAnnotations;

namespace TiendaVirtual.Intercambio.Dto.VendedorXqm
{
    public class CambiarPlanDto
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int NuevoPlanId { get; set; }

        /// <summary>
        /// Obligatorio al bajar a un plan con menos cupo que los productos en uso:
        /// estos se conservan y el resto se pausa.
        /// </summary>
        [MaxLength(1000)]
        public List<int>? ProductoIdsConservar { get; set; }
    }

    public class CotizarCambioPlanDto
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int NuevoPlanId { get; set; }
    }
}
