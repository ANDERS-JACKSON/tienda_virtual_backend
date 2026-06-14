using System.ComponentModel;

namespace TiendaVirtual.Comun.Enumeracion
{
    public enum TipoEstadoContacto
    {
        [Description("NUEVO")]
        Nuevo = 1,

        [Description("LEIDO")]
        Leido = 2,

        [Description("RESPONDIDO")]
        Respondido = 3,

        [Description("ARCHIVADO")]
        Archivado = 4,

        [Description("SPAM")]
        Spam = 5
    }
}
