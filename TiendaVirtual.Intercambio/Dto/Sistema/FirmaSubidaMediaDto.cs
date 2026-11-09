namespace TiendaVirtual.Intercambio.Dto.Sistema
{
    public class SolicitarFirmaMediaDto
    {
        /// <summary>
        /// producto | logo | banner | pdf | kyc | envio | reclamo | resena | carrusel | aviso | imagen
        /// </summary>
        public string Proposito { get; set; } = "imagen";
    }

    public class FirmaSubidaMediaDto
    {
        public bool Activo { get; set; }
        public string? CloudName { get; set; }
        public string? ApiKey { get; set; }
        public long Timestamp { get; set; }
        public string? Signature { get; set; }
        public string? Folder { get; set; }
        public string? ResourceType { get; set; }
    }
}
