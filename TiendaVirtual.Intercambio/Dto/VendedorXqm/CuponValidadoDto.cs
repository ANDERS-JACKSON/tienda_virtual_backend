namespace TiendaVirtual.Intercambio.Dto.VendedorXqm
{
    public class CuponValidadoDto
    {
        public bool Valido { get; set; }
        public string Mensaje { get; set; } = null!;
        public CuponDto? Cupon { get; set; }
        public decimal? PrecioOriginal { get; set; }
        public decimal? PrecioConDescuento { get; set; }
        public int MesesGratisTotal { get; set; }
    }
}
