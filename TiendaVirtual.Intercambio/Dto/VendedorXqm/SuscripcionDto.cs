using TiendaVirtual.Intercambio.Dto.Sistema;

namespace TiendaVirtual.Intercambio.Dto.VendedorXqm
{
    public class SuscripcionDto
    {
        public int SuscripcionId { get; set; }
        public int VendedorId { get; set; }
        public PlanDto Plan { get; set; } = null!;
        public EnumeracionDto Estado { get; set; } = null!;
        public short MesesGratisOtorgados { get; set; }
        public decimal? PrecioPersonalizado { get; set; }
        public decimal PrecioEfectivo { get; set; }
        public CuponDto? Cupon { get; set; }
        public DateTime? PruebaTerminaEn { get; set; }
        public DateTime? PeriodoInicio { get; set; }
        public DateTime? PeriodoFin { get; set; }
        public bool RequierePago { get; set; }
        public bool PuedePublicar { get; set; }
        public int DiasRestantes { get; set; }

        /// <summary>Solo en listado admin.</summary>
        public string? NombreTienda { get; set; }
        public string? CorreoVendedor { get; set; }
    }
}
