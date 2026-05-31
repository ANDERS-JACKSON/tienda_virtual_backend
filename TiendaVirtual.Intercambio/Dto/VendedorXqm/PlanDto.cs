using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Intercambio.Dto.Sistema;

namespace TiendaVirtual.Intercambio.Dto.VendedorXqm
{
    public class PlanDto
    {
        public int PlanId { get; set; }
        public string Codigo { get; set; } = null!;
        public string Nombre { get; set; } = null!;
        public string? Descripcion { get; set; }
        public decimal Precio { get; set; }
        public EnumeracionDto Periodo { get; set; } = null!;
        public int? MaxProductos { get; set; }
        public decimal TasaComision { get; set; }
        public bool Activo { get; set; }
    }
}
