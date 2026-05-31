using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Modelo.SeguridadXqm;
using TiendaVirtual.Dominio.Modelo.VendedorXqm;
using TiendaVirtual.Dominio.Utilidad;

namespace TiendaVirtual.Dominio.Modelo.PagoXqm
{
    public class Retiro
    {
        public int RetiroId { get; set; }

        [Required]
        public int VendedorId { get; set; }

        [Required]
        public int CuentaId { get; set; }

        public decimal Monto { get; set; }

        [EnumValorValido]
        public TipoEstadoRetiro Estado { get; set; }

        public int? ProcesadoPor { get; set; }
        public string? ReferenciaTransferencia { get; set; }
        public DateTime FechaSolicitud { get; set; }
        public DateTime? FechaCompletado { get; set; }

        public virtual Vendedor Vendedor { get; set; } = null!;
        public virtual CuentaBancaria Cuenta { get; set; } = null!;
        public virtual Usuario? ProcesadoPorUsuario { get; set; }
    }
}
