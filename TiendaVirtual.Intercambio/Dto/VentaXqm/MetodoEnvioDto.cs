namespace TiendaVirtual.Intercambio.Dto.VentaXqm
{
    public class MetodoEnvioDto
    {
        public int MetodoEnvioId { get; set; }
        public string Codigo { get; set; } = null!;
        public string Nombre { get; set; } = null!;
        public string? Descripcion { get; set; }
        public decimal MontoBase { get; set; }
        public int TiempoEstimadoDias { get; set; }
    }
}
