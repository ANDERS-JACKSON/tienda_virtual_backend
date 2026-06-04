using System.ComponentModel.DataAnnotations;

namespace TiendaVirtual.Intercambio.Dto.SeguridadXqm
{
    /// <summary>
    /// Solicitud "olvidé mi contraseña": el usuario solo provee su correo.
    /// El servidor genera una clave aleatoria y la envía a esa dirección.
    /// </summary>
    public class RecuperarClaveDto
    {
        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "Formato de correo inválido.")]
        public string Correo { get; set; } = null!;
    }
}
