using System;

namespace TiendaVirtual.Dominio.Modelo.SeguridadXqm
{
    public class UsuarioLoginExterno
    {
        public int UsuarioLoginExternoId { get; set; }
        public int UsuarioId { get; set; }
        public string Proveedor { get; set; } = "GOOGLE";
        public string SubjectId { get; set; } = null!;
        public DateTime FechaVinculacion { get; set; }

        public virtual Usuario Usuario { get; set; } = null!;
    }
}
