using System.ComponentModel;

namespace TiendaVirtual.Comun.Enumeracion
{
    public enum TipoEstadoTransaccion : short
    {
        [Description("PENDIENTE")] Pendiente = 1,
        [Description("PROCESANDO")] Procesando = 2,
        [Description("COMPLETADA")] Completada = 3,
        [Description("FALLIDA")] Fallida = 4,
        [Description("REEMBOLSADA")] Reembolsada = 5,
        [Description("CANCELADA")] Cancelada = 6
    }
}
