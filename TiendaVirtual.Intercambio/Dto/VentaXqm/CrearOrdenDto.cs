using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TiendaVirtual.Intercambio.Dto.VentaXqm
{
    public class MetodoEnvioPorVendedorDto
    {
        [Required] public int VendedorId { get; set; }
        [Required] public int MetodoEnvioId { get; set; }
    }

    public class CrearOrdenDto
    {
        [Required]
        public int DireccionId { get; set; }

        [Required, MinLength(1)]
        public List<MetodoEnvioPorVendedorDto> MetodosEnvio { get; set; } = new();
    }
}
