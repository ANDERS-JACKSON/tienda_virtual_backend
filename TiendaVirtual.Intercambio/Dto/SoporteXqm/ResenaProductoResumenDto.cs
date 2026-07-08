namespace TiendaVirtual.Intercambio.Dto.SoporteXqm
{
    public class ResenaProductoResumenDto
    {
        public decimal Promedio { get; set; }
        public int Total { get; set; }
        /// <summary>Cantidad de reseñas por calificación (clave 1–5).</summary>
        public Dictionary<int, int> Distribucion { get; set; } = new();
    }
}
