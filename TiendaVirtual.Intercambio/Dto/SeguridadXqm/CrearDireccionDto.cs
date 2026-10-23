using System.ComponentModel.DataAnnotations;

namespace TiendaVirtual.Intercambio.Dto.SeguridadXqm
{
    public class CrearDireccionDto
    {
        [MaxLength(50)]
        public string? Etiqueta { get; set; }

        [MaxLength(200)]
        public string? NombreReceptor { get; set; }

        /// <summary>
        /// Documento de quien retira. Si no se envía, se usa el de la cuenta.
        /// </summary>
        [MaxLength(20)]
        public string? DniReceptor { get; set; }

        [MaxLength(20)]
        public string? Telefono { get; set; }

        /// <summary>Código ubigeo INEI del distrito (6 dígitos).</summary>
        [Required, MaxLength(6), MinLength(6)]
        public string DistritoId { get; set; } = null!;

        [Required, MaxLength(300)]
        public string DireccionLinea { get; set; } = null!;

        [MaxLength(300)]
        public string? Referencia { get; set; }

        public bool EsPredeterminada { get; set; }
    }

    public class ActualizarDireccionDto : CrearDireccionDto { }
}
