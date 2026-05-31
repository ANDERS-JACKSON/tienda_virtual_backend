using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Utilidad;

namespace TiendaVirtual.Dominio.Modelo.VendedorXqm
{
    public class Plan
    {
        public int PlanId { get; set; }

        [Required]
        public string Codigo { get; set; } = null!;

        [Required]
        public string Nombre { get; set; } = null!;

        public string? Descripcion { get; set; }
        public decimal Precio { get; set; }

        [EnumValorValido]
        public TipoPeriodoPlan Periodo { get; set; }

        public int? MaxProductos { get; set; }
        public decimal TasaComision { get; set; }
        public bool Activo { get; set; }

        public virtual ICollection<Suscripcion> Suscripciones { get; set; } = new List<Suscripcion>();
    }
}
