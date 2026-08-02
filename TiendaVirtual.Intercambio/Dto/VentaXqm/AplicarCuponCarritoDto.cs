using System.ComponentModel.DataAnnotations;

namespace TiendaVirtual.Intercambio.Dto.VentaXqm
{
    public class AplicarCuponCarritoDto
    {
        [Required]
        [MaxLength(40)]
        public string Codigo { get; set; } = null!;
    }
}
