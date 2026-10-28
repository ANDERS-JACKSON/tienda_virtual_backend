using System;

namespace TiendaVirtual.Dominio.Modelo.SeguridadXqm
{
    public class EventoAuditoria
    {
        public long EventoId { get; set; }
        public DateTime Fecha { get; set; }
        public string Accion { get; set; } = null!;
        public Guid? UsuarioId { get; set; }
        public string? DireccionIp { get; set; }
        public string? Detalle { get; set; }
    }
}
