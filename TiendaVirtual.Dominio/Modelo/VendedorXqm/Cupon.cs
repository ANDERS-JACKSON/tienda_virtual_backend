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
    public class Cupon
    {
        public int CuponId { get; set; }

        [Required]
        public string Codigo { get; set; } = null!;

        [EnumValorValido]
        public TipoDescuentoCupon TipoDescuento { get; set; }

        public decimal? ValorDescuento { get; set; }
        public short MesesGratis { get; set; }
        public int? UsosMaximos { get; set; }
        public int UsosRealizados { get; set; }
        public DateTime? ValidoHasta { get; set; }
        public bool Activo { get; set; }

        public virtual ICollection<Suscripcion> Suscripciones { get; set; } = new List<Suscripcion>();
    }
}
