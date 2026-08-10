namespace TiendaVirtual.Dominio.Opciones
{
    public class IzipayOpciones
    {
        public const string Seccion = "Izipay";

        public string PublicKey { get; set; } = string.Empty;
        public string PrivateKey { get; set; } = string.Empty;
        public string HmacKey { get; set; } = string.Empty;
        public string ApiUrl { get; set; } = string.Empty;

        /// <summary>
        /// Solo válido en Development. En Production el arranque falla si es true.
        /// </summary>
        public bool PermitirConfirmacionDemo { get; set; }
    }
}
