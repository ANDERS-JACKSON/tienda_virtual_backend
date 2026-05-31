using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Intercambio.Dto.Sistema;

namespace TiendaVirtual.Intercambio.Dto.VendedorXqm
{
    public class CuponDto
    {
        public int CuponId { get; set; }
        public string Codigo { get; set; } = null!;
        public EnumeracionDto TipoDescuento { get; set; } = null!;
        public decimal? ValorDescuento { get; set; }
        public short MesesGratis { get; set; }
        public int? UsosMaximos { get; set; }
        public int UsosRealizados { get; set; }
        public DateTime? ValidoHasta { get; set; }
        public bool Activo { get; set; }
    }
}
