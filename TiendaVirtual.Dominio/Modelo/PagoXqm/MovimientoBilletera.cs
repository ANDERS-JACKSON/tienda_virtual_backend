using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Modelo.VendedorXqm;
using TiendaVirtual.Dominio.Utilidad;

namespace TiendaVirtual.Dominio.Modelo.PagoXqm
{
    public class MovimientoBilletera
    {
        public long MovimientoId { get; set; }

        [Required]
        public int VendedorId { get; set; }

        [EnumValorValido]
        public TipoMovimientoBilletera Tipo { get; set; }

        public decimal Monto { get; set; }
        public decimal SaldoResultante { get; set; }
        public long? ReferenciaId { get; set; }
        public string? Descripcion { get; set; }
        public DateTime Fecha { get; set; }

        public virtual Vendedor Vendedor { get; set; } = null!;
    }
}
