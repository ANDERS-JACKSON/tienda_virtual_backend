using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TiendaVirtual.Intercambio.Dto.VendedorXqm
{
    public class PlanBeneficioItemDto
    {
        [Required, MaxLength(200)]
        public string Texto { get; set; } = null!;

        /// <summary>true = check incluido; false = cruz / no incluido.</summary>
        public bool Incluido { get; set; } = true;
    }

    public class PlanBeneficiosDto
    {
        /// <summary>Usada si no hay herencia. Si hay heredaDePlanId, el API rellena EtiquetaResuelta.</summary>
        [MaxLength(80)]
        public string? Etiqueta { get; set; }

        /// <summary>Etiqueta lista para mostrar (incluye “Todo lo del X, más”).</summary>
        public string EtiquetaResuelta { get; set; } = "Incluye";

        public int? HeredaDePlanId { get; set; }

        /// <summary>Nombre del plan base cuando hay herencia (solo lectura en listados).</summary>
        public string? HeredaDePlanNombre { get; set; }

        public bool Destacado { get; set; }

        [MaxLength(200)]
        public string? NotaPie { get; set; }

        public List<PlanBeneficioItemDto> Items { get; set; } = new();
    }
}
