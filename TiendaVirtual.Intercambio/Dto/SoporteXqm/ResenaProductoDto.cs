namespace TiendaVirtual.Intercambio.Dto.SoporteXqm
{
    public class ResenaProductoDto
    {
        public long ResenaId { get; set; }
        public int ProductoId { get; set; }
        public string NombreProducto { get; set; } = null!;
        public int ClienteId { get; set; }
        public string NombreCliente { get; set; } = null!;
        public int Calificacion { get; set; }
        public string? Titulo { get; set; }
        public string? Comentario { get; set; }
        public List<string> Imagenes { get; set; } = new();
        public string? RespuestaVendedor { get; set; }
        public DateTime Fecha { get; set; }
    }
}
