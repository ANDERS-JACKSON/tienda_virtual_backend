using System.ComponentModel.DataAnnotations;
using TiendaVirtual.Intercambio.Dto.Sistema;

namespace TiendaVirtual.Intercambio.Dto.SoporteXqm
{
    public class CrearMensajeContactoDto
    {
        [Required]
        [MaxLength(150)]
        public string Nombre { get; set; } = null!;

        [Required]
        [MaxLength(150)]
        [EmailAddress]
        public string Correo { get; set; } = null!;

        [Required]
        [MaxLength(200)]
        public string Asunto { get; set; } = null!;

        [Required]
        [MaxLength(1000)]
        public string Mensaje { get; set; } = null!;

        /// <summary>Campo honeypot anti-bot. Debe ir vacío.</summary>
        public string? Sitio { get; set; }
    }

    public class MensajeContactoListadoDto
    {
        public long Id { get; set; }
        public string Nombre { get; set; } = null!;
        public string Correo { get; set; } = null!;
        public string Asunto { get; set; } = null!;
        public EnumeracionDto Estado { get; set; } = null!;
        public DateTime FechaMensaje { get; set; }
        public bool FueRespondido { get; set; }
    }

    public class MensajeContactoDetalleDto
    {
        public long Id { get; set; }
        public int? UsuarioId { get; set; }
        public string Nombre { get; set; } = null!;
        public string Correo { get; set; } = null!;
        public string Asunto { get; set; } = null!;
        public string Mensaje { get; set; } = null!;
        public EnumeracionDto Estado { get; set; } = null!;
        public string? Respuesta { get; set; }
        public int? RespondidoPor { get; set; }
        public string? NombreRespondedor { get; set; }
        public DateTime? FechaRespuesta { get; set; }
        public DateTime FechaMensaje { get; set; }
    }

    public class ResponderMensajeContactoDto
    {
        [Required]
        [MaxLength(1000)]
        public string Respuesta { get; set; } = null!;
    }

    public class CambiarEstadoMensajeContactoDto
    {
        public int Estado { get; set; }
    }

    public class ContadorMensajesContactoDto
    {
        public int TotalNoLeidos { get; set; }
    }
}
