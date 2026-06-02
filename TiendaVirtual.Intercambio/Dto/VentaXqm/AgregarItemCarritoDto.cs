using System.ComponentModel.DataAnnotations;

namespace TiendaVirtual.Intercambio.Dto.VentaXqm
{
    public class AgregarItemCarritoDto
    {
        [Required]
        public int VarianteId { get; set; }

        [Range(1, 999)]
        public int Cantidad { get; set; } = 1;
    }

    public class ActualizarItemCarritoDto
    {
        [Range(1, 999)]
        public int Cantidad { get; set; }
    }
}
