using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Modelo.PagoXqm;
using TiendaVirtual.Dominio.Utilidad;

namespace TiendaVirtual.Dominio.Modelo.VendedorXqm
{
    public class Suscripcion
    {
        public int SuscripcionId { get; set; }

        [Required]
        public int VendedorId { get; set; }

        [Required]
        public int PlanId { get; set; }

        [EnumValorValido]
        public TipoEstadoSuscripcion Estado { get; set; }

        public short MesesGratisOtorgados { get; set; }
        public decimal? PrecioPersonalizado { get; set; }
        public int? CuponId { get; set; }
        public DateTime? PruebaTerminaEn { get; set; }
        public DateTime? PeriodoInicio { get; set; }
        public DateTime? PeriodoFin { get; set; }

        public virtual Vendedor Vendedor { get; set; } = null!;
        public virtual Plan Plan { get; set; } = null!;
        public virtual Cupon? Cupon { get; set; }
        public virtual ICollection<Transaccion> Transacciones { get; set; } = new List<Transaccion>();
    }
}
