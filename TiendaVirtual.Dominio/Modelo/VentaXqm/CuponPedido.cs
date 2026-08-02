using System.ComponentModel.DataAnnotations;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Utilidad;

namespace TiendaVirtual.Dominio.Modelo.VentaXqm
{
    /// <summary>
    /// Cupón de descuento aplicable al carrito / pedido (no suscripciones).
    /// </summary>
    public class CuponPedido
    {
        public int CuponPedidoId { get; set; }

        [Required]
        public string Codigo { get; set; } = null!;

        [EnumValorValido]
        public TipoDescuentoCupon TipoDescuento { get; set; }

        public decimal ValorDescuento { get; set; }
        public decimal? MontoMinimo { get; set; }
        public int? UsosMaximos { get; set; }
        public int UsosRealizados { get; set; }
        public DateTime? ValidoHasta { get; set; }
        public bool Activo { get; set; }
        public string? Descripcion { get; set; }
        public DateTime FechaCreacion { get; set; }

        public virtual ICollection<Carrito> Carritos { get; set; } = new List<Carrito>();
        public virtual ICollection<Orden> Ordenes { get; set; } = new List<Orden>();
    }
}
