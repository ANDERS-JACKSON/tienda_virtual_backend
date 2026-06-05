using System.ComponentModel;

namespace TiendaVirtual.Comun.Enumeracion
{
    public enum TipoTransaccion : short
    {
        [Description("PAGO_ORDEN")] PagoOrden = 1,
        [Description("PAGO_SUSCRIPCION")] PagoSuscripcion = 2,
        [Description("REEMBOLSO")] Reembolso = 3
    }
}
