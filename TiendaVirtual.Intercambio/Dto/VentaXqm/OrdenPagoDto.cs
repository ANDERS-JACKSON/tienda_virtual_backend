using System.ComponentModel.DataAnnotations;

namespace TiendaVirtual.Intercambio.Dto.VentaXqm
{
    public class IniciarPagoOrdenDto
    {
        [Required]
        public long OrdenId { get; set; }
    }

    public class ConfirmarPagoOrdenDto
    {
        [Required]
        public long TransaccionId { get; set; }

        [Required]
        public string TransaccionProveedorId { get; set; } = null!;

        [Required]
        public string MetodoPago { get; set; } = null!;

        public string? RespuestaProveedor { get; set; }

        public bool Exitosa { get; set; }
    }

    /// <summary>Respuesta al iniciar cobro de una orden (Izipay / demo).</summary>
    public class RespuestaInicioPagoOrdenDto
    {
        public long TransaccionId { get; set; }
        public decimal Monto { get; set; }
        public string Moneda { get; set; } = "PEN";
        public string Concepto { get; set; } = null!;
        public string FormToken { get; set; } = null!;
        public string PublicKey { get; set; } = null!;
    }

    public class TransaccionOrdenDto
    {
        public long TransaccionId { get; set; }
        public long? OrdenId { get; set; }
        public string Proveedor { get; set; } = null!;
        public string? TransaccionProveedorId { get; set; }
        public Dto.Sistema.EnumeracionDto Tipo { get; set; } = null!;
        public decimal Monto { get; set; }
        public Dto.Sistema.EnumeracionDto Estado { get; set; } = null!;
        public string? MetodoPago { get; set; }
        public DateTime Fecha { get; set; }
    }
}
