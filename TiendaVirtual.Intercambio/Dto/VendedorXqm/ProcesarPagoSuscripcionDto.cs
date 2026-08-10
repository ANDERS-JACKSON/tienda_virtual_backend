using System.ComponentModel.DataAnnotations;

namespace TiendaVirtual.Intercambio.Dto.VendedorXqm
{
    /// <summary>
    /// Cobro de suscripción con token MP. Nunca incluir datos de tarjeta en claro.
    /// </summary>
    public class ProcesarPagoSuscripcionDto
    {
        [Required]
        public Guid TransaccionId { get; set; }

        [Required]
        [MaxLength(256)]
        public string Token { get; set; } = null!;

        [Required]
        [MaxLength(64)]
        public string PaymentMethodId { get; set; } = null!;

        [Range(1, 48)]
        public int Installments { get; set; } = 1;

        [MaxLength(32)]
        public string? IssuerId { get; set; }

        [EmailAddress]
        [MaxLength(256)]
        public string? PayerEmail { get; set; }

        [MaxLength(16)]
        public string? IdentificationType { get; set; }

        [MaxLength(32)]
        public string? IdentificationNumber { get; set; }
    }
}
