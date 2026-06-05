namespace TiendaVirtual.Intercambio.Dto.VendedorXqm
{
    public class RespuestaInicioPagoDto
    {
        public long TransaccionId { get; set; }
        public decimal Monto { get; set; }
        public string Moneda { get; set; } = "PEN";
        public string Concepto { get; set; } = null!;
        public string FormToken { get; set; } = null!;
        public string PublicKey { get; set; } = null!;
    }
}
