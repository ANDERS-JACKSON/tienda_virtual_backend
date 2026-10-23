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
        public Guid DireccionId { get; set; }

        /// <summary>
        /// Opcional. Si se envía, se usa el método indicado por vendedor.
        /// Si no, el backend asigna el método por defecto (SHALOM). El costo
        /// del envío NO se cobra en la orden — el comprador paga en la agencia.
        /// </summary>
        public List<MetodoEnvioPorVendedorDto>? MetodosEnvio { get; set; }

        /// <summary>Si es true, otra persona retira el pedido en la agencia.</summary>
        public bool RetiraOtraPersona { get; set; }

        [MaxLength(200)]
        public string? NombreReceptor { get; set; }

        [MaxLength(20)]
        public string? DniReceptor { get; set; }

        [MaxLength(20)]
        public string? TelefonoReceptor { get; set; }
    }
}
