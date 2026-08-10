namespace TiendaVirtual.Dominio.Servicios.PagoXqm.Modelos
{
    public sealed class PreparacionCheckoutSolicitud
    {
        public required Guid TransaccionId { get; init; }
        public required decimal Monto { get; init; }
        public required string Moneda { get; init; }
        public required string Concepto { get; init; }
        public required string EmailCliente { get; init; }
        public string? NumeroDocumento { get; init; }
    }

    public sealed class PreparacionCheckoutResultado
    {
        public required string CodigoProveedor { get; init; }
        public string? PublicKey { get; init; }
        /// <summary>Solo proveedores con formulario embebido tipo Izipay.</summary>
        public string? FormToken { get; init; }
        /// <summary>true = el cliente debe tokenizar (Checkout API) y llamar a procesar-pago.</summary>
        public bool RequiereTokenizacionCliente { get; init; }
        /// <summary>true = el cliente puede confirmar en modo demo (solo Development + Izipay).</summary>
        public bool PermiteConfirmacionDemo { get; init; }
    }

    public sealed class SolicitudPagoDto
    {
        public required Guid TransaccionId { get; init; }
        /// <summary>Clave de idempotencia estable por intento de cobro (no por request HTTP).</summary>
        public required string IdempotencyKey { get; init; }
        public required decimal Monto { get; init; }
        public required string Moneda { get; init; }
        public required string Descripcion { get; init; }
        public required string EmailPagador { get; init; }
        /// <summary>Token de un solo uso (tarjeta o Yape). Nunca PAN/CVV.</summary>
        public required string Token { get; init; }
        public required string PaymentMethodId { get; init; }
        public int Installments { get; init; } = 1;
        public string? IssuerId { get; init; }
        public string? IdentificationType { get; init; }
        public string? IdentificationNumber { get; init; }
        /// <summary>URL pública del webhook (notification_url de MP).</summary>
        public string? NotificationUrl { get; init; }
    }

    public sealed class ResultadoPagoDto
    {
        public bool Exitoso { get; init; }
        public bool Pendiente { get; init; }
        public string? IdPagoExterno { get; init; }
        public string? EstadoExterno { get; init; }
        public string? StatusDetail { get; init; }
        public decimal? MontoConfirmado { get; init; }
        public string? MetodoPago { get; init; }
        public string? RespuestaCruda { get; init; }
        public string? MensajeError { get; init; }
    }

    public sealed class EstadoPagoDto
    {
        public string? IdPagoExterno { get; init; }
        public string? EstadoExterno { get; init; }
        public string? StatusDetail { get; init; }
        public bool Exitoso { get; init; }
        public bool Pendiente { get; init; }
        /// <summary>
        /// True cuando no se pudo consultar al proveedor (timeout/5xx/red).
        /// No significa rechazo: no cancelar ni reembolsar en este caso.
        /// </summary>
        public bool ConsultaFallida { get; init; }
        public decimal? MontoConfirmado { get; init; }
        public string? ExternalReference { get; init; }
        public string? MetodoPago { get; init; }
        public string? RespuestaCruda { get; init; }
    }

    public sealed class ResultadoReembolsoDto
    {
        public bool Exitoso { get; init; }
        public string? IdReembolsoExterno { get; init; }
        public decimal? MontoReembolsado { get; init; }
        public string? EstadoExterno { get; init; }
        public string? RespuestaCruda { get; init; }
        public string? MensajeError { get; init; }
    }

    /// <summary>Resultado ya verificado (firma + GET al proveedor) listo para aplicar en negocio.</summary>
    public sealed class ResultadoPagoVerificadoDto
    {
        public required Guid TransaccionId { get; init; }
        public required string IdPagoExterno { get; init; }
        public required bool Exitoso { get; init; }
        public bool Pendiente { get; init; }
        public decimal MontoConfirmado { get; init; }
        public string? MetodoPago { get; init; }
        public string? EstadoExterno { get; init; }
        public string? StatusDetail { get; init; }
        public string? RespuestaCruda { get; init; }
    }
}
