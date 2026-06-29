namespace TiendaVirtual.Intercambio.Dto.Sistema
{
    /// <summary>Tipo de documento de identidad con metadatos para validación en UI.</summary>
    public class TipoDocumentoIdentidadDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Etiqueta { get; set; } = string.Empty;
        public int LongitudDocumento { get; set; }
    }
}
