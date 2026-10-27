using System.ComponentModel.DataAnnotations;

namespace TiendaVirtual.Intercambio.Dto.VendedorXqm
{
    /// <summary>Consulta estado del cobro de una suscripción pendiente (anti doble cobro).</summary>
    public class VerificarPagoSuscripcionDto
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int SuscripcionId { get; set; }

        [Range(1, int.MaxValue)]
        public int? PlanDestinoId { get; set; }
    }

    public class ResultadoVerificarPagoSuscripcionDto
    {
        public int SuscripcionId { get; set; }
        public bool Pagado { get; set; }
        public bool Pendiente { get; set; }
        /// <summary>True si se puede abrir el checkout y crear un cobro nuevo (sin cargo en curso).</summary>
        public bool PuedeReintentarCobro { get; set; }
        public string? MensajeUsuario { get; set; }
        public Guid? TransaccionId { get; set; }
        public decimal Monto { get; set; }
        public string Proveedor { get; set; } = null!;
        public string? PublicKey { get; set; }
        public bool RequiereTokenizacionCliente { get; set; }
        public bool PermiteConfirmacionDemo { get; set; }
        public string Moneda { get; set; } = "PEN";
        public string? Concepto { get; set; }
    }
}
