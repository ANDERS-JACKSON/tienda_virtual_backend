using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Intercambio.Dto.Sistema;

namespace TiendaVirtual.Intercambio.Dto.PagoXqm
{
    public class TransaccionDto
    {
        public long TransaccionId { get; set; }
        public long? OrdenId { get; set; }
        public int? SuscripcionId { get; set; }
        public int UsuarioId { get; set; }
        public string Proveedor { get; set; } = null!;
        public string? TransaccionProveedorId { get; set; }
        public EnumeracionDto Tipo { get; set; } = null!;
        public decimal Monto { get; set; }
        public EnumeracionDto Estado { get; set; } = null!;
        public string? MetodoPago { get; set; }
        public string? RespuestaProveedor { get; set; }
        public DateTime Fecha { get; set; }
    }
}
