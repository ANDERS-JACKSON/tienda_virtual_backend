namespace TiendaVirtual.Intercambio.Dto.SoporteXqm
{
    public class ResenaVendedorDto
    {
        public long ResenaId { get; set; }
        public int VendedorId { get; set; }
        public int ClienteId { get; set; }
        public string NombreCliente { get; set; } = null!;
        public int Calificacion { get; set; }
        public string? Comentario { get; set; }
        public DateTime Fecha { get; set; }
    }
}
