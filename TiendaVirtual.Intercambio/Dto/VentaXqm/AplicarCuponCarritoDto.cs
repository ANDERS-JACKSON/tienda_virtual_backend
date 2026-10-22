using System.ComponentModel.DataAnnotations;

namespace TiendaVirtual.Intercambio.Dto.VentaXqm
{
    public class AplicarCuponCarritoDto
    {
        [Required]
        [MaxLength(40)]
        public string Codigo { get; set; } = null!;
    }

    public class RevisarCuponCarritoDto
    {
        [Required]
        [MaxLength(40)]
        public string Codigo { get; set; } = null!;

        public decimal Subtotal { get; set; }
    }

    public class RevisionCuponCarritoDto
    {
        public string Codigo { get; set; } = null!;
        public bool RequiereAutenticacion { get; set; }
    }
}
