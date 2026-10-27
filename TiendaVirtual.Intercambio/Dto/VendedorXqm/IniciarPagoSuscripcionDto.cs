using System.ComponentModel.DataAnnotations;

namespace TiendaVirtual.Intercambio.Dto.VendedorXqm
{
    public class IniciarPagoSuscripcionDto
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int SuscripcionId { get; set; }

        /// <summary>Si se indica, el cobro es para subir a este plan (monto calculado en servidor).</summary>
        [Range(1, int.MaxValue)]
        public int? PlanDestinoId { get; set; }
    }
}
