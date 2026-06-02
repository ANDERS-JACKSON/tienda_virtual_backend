using System;
using TiendaVirtual.Intercambio.Dto.Sistema;

namespace TiendaVirtual.Intercambio.Dto.VentaXqm
{
    public class OrdenListadoDto
    {
        public long OrdenId { get; set; }
        public string NumeroOrden { get; set; } = null!;
        public decimal Total { get; set; }
        public EnumeracionDto Estado { get; set; } = null!;
        public DateTime Fecha { get; set; }
        public int TotalItems { get; set; }
        public int TotalVendedores { get; set; }
        public string? ImagenPrincipalUrl { get; set; }
    }
}
