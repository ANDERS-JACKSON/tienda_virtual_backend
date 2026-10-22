using System.Collections.Generic;

namespace TiendaVirtual.Intercambio.Dto.VentaXqm
{
    public class CotizarCarritoDto
    {
        public List<AgregarItemCarritoDto> Items { get; set; } = new();

        /// <summary>Cupón guardado en el navegador. Se aplica al fusionar si la sesión ya existe.</summary>
        public string? CodigoCupon { get; set; }
    }

    public class ResultadoFusionCarritoDto
    {
        public CarritoDto Carrito { get; set; } = new();
        public List<int> VariantesAgregadas { get; set; } = new();
        public List<string> Advertencias { get; set; } = new();
    }
}
