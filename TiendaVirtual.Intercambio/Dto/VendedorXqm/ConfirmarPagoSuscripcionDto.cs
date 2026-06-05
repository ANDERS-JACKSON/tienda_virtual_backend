using System.ComponentModel.DataAnnotations;

namespace TiendaVirtual.Intercambio.Dto.VendedorXqm
{
    public class ConfirmarPagoSuscripcionDto
    {
        [Required]
        public long TransaccionId { get; set; }

        [Required]
        public string TransaccionProveedorId { get; set; } = null!;

        [Required]
        public string MetodoPago { get; set; } = null!;

        public string? RespuestaProveedor { get; set; }

        public bool Exitosa { get; set; }
    }
}
