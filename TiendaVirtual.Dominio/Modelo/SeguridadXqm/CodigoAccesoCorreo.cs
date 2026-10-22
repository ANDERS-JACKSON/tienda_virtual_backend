using System;

namespace TiendaVirtual.Dominio.Modelo.SeguridadXqm
{
    /// <summary>
    /// Código de un solo uso enviado por correo. Se guarda solo el hash.
    /// </summary>
    public class CodigoAccesoCorreo
    {
        public Guid CodigoAccesoId { get; set; }
        public string Correo { get; set; } = null!;
        public string CodigoHash { get; set; } = null!;
        public DateTime ExpiraEn { get; set; }
        public short Intentos { get; set; }
        public bool Usado { get; set; }
        public DateTime FechaCreacion { get; set; }
        public string? DireccionIp { get; set; }
    }
}
