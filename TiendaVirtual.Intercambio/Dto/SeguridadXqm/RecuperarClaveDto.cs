using System.ComponentModel.DataAnnotations;

namespace TiendaVirtual.Intercambio.Dto.SeguridadXqm
{
    /// <summary>
    /// Pide un código de 6 dígitos para elegir una contraseña nueva.
    /// </summary>
    public class RecuperarClaveDto
    {
        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "Formato de correo inválido.")]
        public string Correo { get; set; } = null!;
    }

    public class VerificarRecuperacionClaveDto
    {
        [Required]
        [EmailAddress]
        public string Correo { get; set; } = null!;

        [Required]
        public string Codigo { get; set; } = null!;
    }

    public class ConfirmarRecuperacionClaveDto
    {
        [Required]
        [EmailAddress]
        public string Correo { get; set; } = null!;

        [Required]
        public string Ticket { get; set; } = null!;

        [Required]
        public string ContrasenaNueva { get; set; } = null!;
    }
}
