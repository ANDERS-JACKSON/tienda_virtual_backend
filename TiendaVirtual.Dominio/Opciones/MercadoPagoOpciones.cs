namespace TiendaVirtual.Dominio.Opciones
{
    /// <summary>
    /// Credenciales vía user-secrets / variables de entorno (MercadoPago__AccessToken, etc.).
    /// Nunca versionar secretos reales en appsettings.json.
    /// </summary>
    public class MercadoPagoOpciones
    {
        public const string Seccion = "MercadoPago";

        public string AccessToken { get; set; } = string.Empty;
        public string PublicKey { get; set; } = string.Empty;
        public string WebhookSecret { get; set; } = string.Empty;
        /// <summary>URL pública del API (para documentar notification_url).</summary>
        public string UrlBase { get; set; } = string.Empty;
    }
}
