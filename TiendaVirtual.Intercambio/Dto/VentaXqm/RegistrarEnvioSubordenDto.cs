using System.ComponentModel.DataAnnotations;

namespace TiendaVirtual.Intercambio.Dto.VentaXqm
{
    public class RegistrarEnvioSubordenDto
    {
        [Required, MaxLength(50)]
        public string Transportista { get; set; } = null!;

        [MaxLength(50)]
        public string? CodigoOrdenAgencia { get; set; }

        [MaxLength(100)]
        public string? NumeroSeguimiento { get; set; }

        [MaxLength(50)]
        public string? ClaveRecojo { get; set; }

        [MaxLength(500)]
        public string? Detalles { get; set; }

        [Required, MaxLength(500)]
        public string ComprobanteUrl { get; set; } = null!;

        [Range(0, 999999.99)]
        public decimal? MontoComprobante { get; set; }
    }
}
