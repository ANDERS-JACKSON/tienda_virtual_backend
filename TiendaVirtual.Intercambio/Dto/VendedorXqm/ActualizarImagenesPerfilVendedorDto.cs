using System.ComponentModel.DataAnnotations;

namespace TiendaVirtual.Intercambio.Dto.VendedorXqm
{
    /// <summary>
    /// Actualización parcial de logo/banner tras subir a Cloudinary.
    /// Solo se modifican los campos enviados con valor.
    /// </summary>
    public class ActualizarImagenesPerfilVendedorDto
    {
        [MaxLength(500)]
        public string? LogoUrl { get; set; }

        [MaxLength(500)]
        public string? BannerUrl { get; set; }
    }
}
