using System.ComponentModel.DataAnnotations;
using TiendaVirtual.Intercambio.Dto.Sistema;

namespace TiendaVirtual.Intercambio.Dto.VendedorXqm
{
    /// <summary>
    /// Actualización de plan. El código es inmutable y no se acepta aquí.
    /// </summary>
    public class ActualizarPlanDto
    {
        [Required, MaxLength(100)]
        public string Nombre { get; set; } = null!;

        [MaxLength(500)]
        public string? Descripcion { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal Precio { get; set; }

        [Required]
        public EnumeracionDto Periodo { get; set; } = null!;

        [Range(1, int.MaxValue)]
        public int? MaxProductos { get; set; }

        [Range(0, 100)]
        public decimal TasaComision { get; set; }

        public bool Activo { get; set; }

        public PlanBeneficiosDto? Beneficios { get; set; }
    }
}
