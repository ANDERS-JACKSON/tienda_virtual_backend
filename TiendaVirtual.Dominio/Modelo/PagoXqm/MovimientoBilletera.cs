using System;
using System.ComponentModel.DataAnnotations;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Modelo.VendedorXqm;
using TiendaVirtual.Dominio.Utilidad;

namespace TiendaVirtual.Dominio.Modelo.PagoXqm
{
    public class MovimientoBilletera
    {
        public Guid MovimientoId { get; set; }

        [Required]
        public int VendedorId { get; set; }

        [EnumValorValido]
        public TipoMovimientoBilletera Tipo { get; set; }

        public decimal Monto { get; set; }
        public decimal SaldoResultante { get; set; }
        public Guid? ReferenciaId { get; set; }
        public string? Descripcion { get; set; }
        public DateTime Fecha { get; set; }

        public virtual Vendedor Vendedor { get; set; } = null!;
    }
}
