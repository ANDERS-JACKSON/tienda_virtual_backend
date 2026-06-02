using System.ComponentModel;

namespace TiendaVirtual.Comun.Enumeracion
{
    public enum TipoEstadoOrden : short
    {
        [Description("PENDIENTE_PAGO")]
        PendientePago = 1,

        [Description("PAGADA")]
        Pagada = 2,

        [Description("EN_PREPARACION")]
        EnPreparacion = 3,

        [Description("EN_CAMINO")]
        EnCamino = 4,

        [Description("ENTREGADA")]
        Entregada = 5,

        [Description("CANCELADA")]
        Cancelada = 6,

        [Description("DISPUTADA")]
        Disputada = 7
    }
}
