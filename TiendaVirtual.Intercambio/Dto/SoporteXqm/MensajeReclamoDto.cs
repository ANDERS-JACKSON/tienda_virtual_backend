namespace TiendaVirtual.Intercambio.Dto.SoporteXqm
{
    public class MensajeReclamoDto
    {
        public long MensajeId { get; set; }
        public int RemitenteId { get; set; }
        public string NombreRemitente { get; set; } = null!;
        public string RolRemitente { get; set; } = null!;
        public string Mensaje { get; set; } = null!;
        public List<string> Adjuntos { get; set; } = new();
        public DateTime Fecha { get; set; }
    }
}
