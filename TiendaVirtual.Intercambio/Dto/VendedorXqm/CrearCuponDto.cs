using System.ComponentModel.DataAnnotations;
using TiendaVirtual.Intercambio.Dto.Sistema;

namespace TiendaVirtual.Intercambio.Dto.VendedorXqm
{
    public class CrearCuponDto
    {
        [Required, MaxLength(50)]
        public string Codigo { get; set; } = null!;

        [Required]
        public EnumeracionDto TipoDescuento { get; set; } = null!;

        public decimal? ValorDescuento { get; set; }

        [Range(0, 24)]
        public int MesesGratis { get; set; }

        [Range(1, int.MaxValue)]
        public int? UsosMaximos { get; set; }

        public DateTime? ValidoHasta { get; set; }
    }
}
