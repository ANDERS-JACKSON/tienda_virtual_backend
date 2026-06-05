using System.ComponentModel.DataAnnotations;

namespace TiendaVirtual.Intercambio.Dto.VendedorXqm
{
    public class IniciarPagoSuscripcionDto
    {
        [Required]
        public int SuscripcionId { get; set; }
    }
}
