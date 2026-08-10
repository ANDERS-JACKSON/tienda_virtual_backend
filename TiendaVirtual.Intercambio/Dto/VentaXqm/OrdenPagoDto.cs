using System.ComponentModel.DataAnnotations;

namespace TiendaVirtual.Intercambio.Dto.VentaXqm
{
    /// <summary>
    /// Datos públicos del proveedor activo para abrir el modal de pago
    /// sin crear todavía la orden.
    /// </summary>
    public class ConfiguracionCheckoutPagoDto
    {
        public string Proveedor { get; set; } = null!;
        public string? PublicKey { get; set; }
        public bool RequiereTokenizacionCliente { get; set; }
        public bool PermiteConfirmacionDemo { get; set; }
        public decimal MontoEstimado { get; set; }
        public string Moneda { get; set; } = "PEN";
    }

    /// <summary>
    /// Cobro atómico: crea la orden + procesa el pago con token de Mercado Pago.
    /// Si el cobro falla, la orden se anula y el carrito se conserva.
    /// </summary>
    public class CobrarCarritoDto
    {
        [Required]
        public Guid DireccionId { get; set; }

        /// <summary>
        /// Token de un solo uso (MercadoPago.js). Opcional solo si el proveedor
        /// admite confirmación demo en Development.
        /// </summary>
        [MaxLength(256)]
        public string? Token { get; set; }

        [MaxLength(64)]
        public string? PaymentMethodId { get; set; }

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

        /// <summary>Solo Izipay demo en Development.</summary>
        public bool ConfirmarDemo { get; set; }
    }

    public class ResultadoCobrarCarritoDto
    {
        public Guid OrdenId { get; set; }
        public string NumeroOrden { get; set; } = null!;
        public Guid TransaccionId { get; set; }
        public string Proveedor { get; set; } = null!;
        public string? TransaccionProveedorId { get; set; }
        public decimal Monto { get; set; }
        public string? MetodoPago { get; set; }
        public string? MensajeUsuario { get; set; }
        public bool Pagado { get; set; }
        public bool Pendiente { get; set; }
    }

    public class IniciarPagoOrdenDto
    {
        [Required]
        public Guid OrdenId { get; set; }
    }

    public class ConfirmarPagoOrdenDto
    {
        [Required]
        public Guid TransaccionId { get; set; }

        [Required]
        public string TransaccionProveedorId { get; set; } = null!;

        [Required]
        public string MetodoPago { get; set; } = null!;

        public string? RespuestaProveedor { get; set; }

        public bool Exitosa { get; set; }
    }

    /// <summary>
    /// Procesa cobro con token de Mercado Pago (tarjeta o Yape).
    /// Nunca incluir número de tarjeta, CVV ni fecha de expiración.
    /// </summary>
    public class ProcesarPagoOrdenDto
    {
        [Required]
        public Guid TransaccionId { get; set; }

        /// <summary>Token de un solo uso generado por MercadoPago.js / Yape.</summary>
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

    /// <summary>Respuesta al iniciar cobro de una orden.</summary>
    public class RespuestaInicioPagoOrdenDto
    {
        public Guid TransaccionId { get; set; }
        public decimal Monto { get; set; }
        public string Moneda { get; set; } = "PEN";
        public string Concepto { get; set; } = null!;
        public string Proveedor { get; set; } = null!;
        public string? FormToken { get; set; }
        public string? PublicKey { get; set; }
        public bool RequiereTokenizacionCliente { get; set; }
        public bool PermiteConfirmacionDemo { get; set; }
    }

    public class ResultadoProcesarPagoOrdenDto
    {
        public Guid TransaccionId { get; set; }
        public Guid? OrdenId { get; set; }
        public string Proveedor { get; set; } = null!;
        public string? TransaccionProveedorId { get; set; }
        public Dto.Sistema.EnumeracionDto Estado { get; set; } = null!;
        public decimal Monto { get; set; }
        public string? MetodoPago { get; set; }
        public string? EstadoExterno { get; set; }
        public string? StatusDetail { get; set; }
        public string? MensajeUsuario { get; set; }
        public bool Pagado { get; set; }
        public bool Pendiente { get; set; }
    }

    public class TransaccionOrdenDto
    {
        public Guid TransaccionId { get; set; }
        public Guid? OrdenId { get; set; }
        public string Proveedor { get; set; } = null!;
        public string? TransaccionProveedorId { get; set; }
        public Dto.Sistema.EnumeracionDto Tipo { get; set; } = null!;
        public decimal Monto { get; set; }
        public Dto.Sistema.EnumeracionDto Estado { get; set; } = null!;
        public string? MetodoPago { get; set; }
        public DateTime Fecha { get; set; }
    }

    /// <summary>Consulta estado del cobro de una orden pendiente (anti doble cobro).</summary>
    public class VerificarPagoOrdenDto
    {
        [Required]
        public Guid OrdenId { get; set; }
    }

    public class ResultadoVerificarPagoOrdenDto
    {
        public Guid OrdenId { get; set; }
        public string NumeroOrden { get; set; } = null!;
        public bool Pagado { get; set; }
        public bool Pendiente { get; set; }
        /// <summary>True si se puede abrir el modal y crear un cobro nuevo (sin cargo en curso).</summary>
        public bool PuedeReintentarCobro { get; set; }
        public string? MensajeUsuario { get; set; }
        public Guid? TransaccionId { get; set; }
        public decimal Monto { get; set; }
        public string Proveedor { get; set; } = null!;
        public string? PublicKey { get; set; }
        public bool RequiereTokenizacionCliente { get; set; }
        public bool PermiteConfirmacionDemo { get; set; }
        public string Moneda { get; set; } = "PEN";
    }
}
