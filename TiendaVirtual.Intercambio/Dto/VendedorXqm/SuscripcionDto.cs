using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Intercambio.Dto.Sistema;

namespace TiendaVirtual.Intercambio.Dto.VendedorXqm
{
    public class SuscripcionDto
    {
        public int SuscripcionId { get; set; }
        public int VendedorId { get; set; }
        public int PlanId { get; set; }
        public EnumeracionDto Estado { get; set; } = null!;
        public short MesesGratisOtorgados { get; set; }
        public decimal? PrecioPersonalizado { get; set; }
        public int? CuponId { get; set; }
        public DateTime? PruebaTerminaEn { get; set; }
        public DateTime? PeriodoInicio { get; set; }
        public DateTime? PeriodoFin { get; set; }
    }
}
