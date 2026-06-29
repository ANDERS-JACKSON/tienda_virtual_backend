using System.ComponentModel.DataAnnotations;

namespace TiendaVirtual.Intercambio.Dto.VentaXqm
{
    public class MarcarListoParaRecogerDto
    {
        [MaxLength(500)]
        public string? Detalles { get; set; }
    }
}
