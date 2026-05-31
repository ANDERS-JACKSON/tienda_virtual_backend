using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Modelo.SeguridadXqm;
using TiendaVirtual.Dominio.Modelo.VendedorXqm;
using TiendaVirtual.Dominio.Modelo.VentaXqm;
using TiendaVirtual.Dominio.Utilidad;

namespace TiendaVirtual.Dominio.Modelo.PagoXqm
{
    public class Transaccion
    {
        public long TransaccionId { get; set; }
        public long? OrdenId { get; set; }
        public int? SuscripcionId { get; set; }

        [Required]
        public int UsuarioId { get; set; }

        [Required]
        public string Proveedor { get; set; } = null!;

        public string? TransaccionProveedorId { get; set; }

        [EnumValorValido]
        public TipoTransaccion Tipo { get; set; }

        public decimal Monto { get; set; }

        [EnumValorValido]
        public TipoEstadoTransaccion Estado { get; set; }

        public string? MetodoPago { get; set; }
        public string? RespuestaProveedor { get; set; }   // JSONB
        public DateTime Fecha { get; set; }

        public virtual Orden? Orden { get; set; }
        public virtual Suscripcion? Suscripcion { get; set; }
        public virtual Usuario Usuario { get; set; } = null!;
    }
}
