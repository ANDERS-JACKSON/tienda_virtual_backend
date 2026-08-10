using TiendaVirtual.Dominio.Servicios.PagoXqm.Modelos;

namespace TiendaVirtual.Dominio.Servicios.PagoXqm
{
    /// <summary>
    /// Contrato de pasarela. OrdenPagoServicio / SuscripcionPagoServicio solo hablan con esta interfaz.
    /// </summary>
    public interface IProveedorPagoServicio
    {
        string CodigoProveedor { get; }

        /// <summary>
        /// Datos que el frontend necesita para capturar el medio de pago
        /// (public key, form token demo, etc.). No cobra.
        /// </summary>
        Task<PreparacionCheckoutResultado> PrepararCheckoutAsync(
            PreparacionCheckoutSolicitud solicitud,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Crea el cobro en el proveedor (token de tarjeta/Yape, etc.).
        /// Nunca debe recibir PAN/CVV en claro.
        /// </summary>
        Task<ResultadoPagoDto> CrearPagoAsync(
            SolicitudPagoDto solicitud,
            CancellationToken cancellationToken = default);

        Task<EstadoPagoDto> ConsultarPagoAsync(
            string idPagoExterno,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Valida autenticidad del webhook (firma). No interpreta el body como verdad del pago.
        /// </summary>
        Task<bool> ValidarFirmaWebhookAsync(
            IReadOnlyDictionary<string, string> headers,
            IReadOnlyDictionary<string, string> query,
            string cuerpoCrudo,
            CancellationToken cancellationToken = default);

        Task<ResultadoReembolsoDto> ReembolsarAsync(
            string idPagoExterno,
            decimal? montoParcial = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Cancela un pago pendiente/en proceso en el proveedor (anti bloqueo).
        /// Retorna false si no aplica o falla.
        /// </summary>
        Task<bool> CancelarPagoAsync(
            string idPagoExterno,
            CancellationToken cancellationToken = default);
    }
}
