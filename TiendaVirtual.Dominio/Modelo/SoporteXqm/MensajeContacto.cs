using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Modelo.SeguridadXqm;
using TiendaVirtual.Dominio.Utilidad;

namespace TiendaVirtual.Dominio.Modelo.SoporteXqm
{
    public class MensajeContacto
    {
        public long MensajeContactoId { get; set; }
        public int? UsuarioId { get; set; }
        public string Nombre { get; set; } = null!;
        public string Correo { get; set; } = null!;
        public string Asunto { get; set; } = null!;
        public string Mensaje { get; set; } = null!;

        [EnumValorValido]
        public TipoEstadoContacto Estado { get; set; }

        public int? RespondidoPor { get; set; }
        public string? Respuesta { get; set; }
        public DateTime? FechaRespuesta { get; set; }
        public DateTime FechaMensaje { get; set; }

        public virtual Usuario? Usuario { get; set; }
        public virtual Usuario? RespondidoPorUsuario { get; set; }
    }
}
