using System.ComponentModel;

namespace TiendaVirtual.Comun.Enumeracion
{
    public enum TipoEstadoSuscripcion : short
    {
        [Description("EN_PRUEBA")] EnPrueba = 1,
        [Description("ACTIVA")] Activa = 2,
        [Description("PENDIENTE_PAGO")] PendientePago = 3,
        [Description("CANCELADA")] Cancelada = 4,
        [Description("SUSPENDIDA")] Suspendida = 5,
        [Description("VENCIDA")] Vencida = 6
    }
}
