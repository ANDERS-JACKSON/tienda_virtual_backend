using Microsoft.EntityFrameworkCore;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Modelo.VendedorXqm;

namespace TiendaVirtual.Dominio.Servicios.SuscripcionXqm.Implementacion
{
    internal static class SuscripcionBeneficiosHelper
    {
        public const int MesesPruebaPrimeraVez = 3;

        public static async Task<bool> EsPrimeraSuscripcionAsync(TiendaVirtualDbContext context, int vendedorId) =>
            !await context.Suscripciones.AnyAsync(s => s.VendedorId == vendedorId);

        /// <summary>Canceló o ya tuvo una suscripción vencida: sin meses gratis al volver.</summary>
        public static async Task<bool> PerdioBeneficioMesesGratisAsync(TiendaVirtualDbContext context, int vendedorId) =>
            await context.Suscripciones.AnyAsync(s =>
                s.VendedorId == vendedorId &&
                (s.Estado == TipoEstadoSuscripcion.Cancelada ||
                 s.Estado == TipoEstadoSuscripcion.Vencida));

        public static async Task<bool> ElegibleMesesGratisAsync(TiendaVirtualDbContext context, int vendedorId)
        {
            if (!await EsPrimeraSuscripcionAsync(context, vendedorId))
                return false;
            return !await PerdioBeneficioMesesGratisAsync(context, vendedorId);
        }

        public static (int MesesPruebaBase, int MesesExtraCupon, string? ErrorCuponMesesGratis) CalcularMesesGratis(
            bool elegibleMesesGratis,
            Cupon? cupon)
        {
            if (!elegibleMesesGratis)
            {
                if (cupon?.TipoDescuento == TipoDescuentoCupon.MesesGratis)
                    return (0, 0,
                        "Si cancelaste antes o ya tuviste una suscripción, no aplican meses gratis. " +
                        "Solo cupones de descuento en el pago mensual.");
                return (0, 0, null);
            }

            var mesesBase = MesesPruebaPrimeraVez;
            var mesesExtra = cupon?.TipoDescuento == TipoDescuentoCupon.MesesGratis ? cupon.MesesGratis : 0;
            return (mesesBase, mesesExtra, null);
        }

        /// <summary>
        /// Plan comercial vigente: EnPrueba o Activa dentro de fechas. Cancelada/Vencida/etc. no publican ni se muestran en catálogo.
        /// </summary>
        public static bool EsComercialmenteActiva(
            TipoEstadoSuscripcion estado,
            DateTime? periodoFin,
            DateTime? pruebaTerminaEn,
            DateTime now) =>
            (estado == TipoEstadoSuscripcion.EnPrueba &&
             pruebaTerminaEn.HasValue &&
             pruebaTerminaEn > now) ||
            (estado == TipoEstadoSuscripcion.Activa &&
             (!periodoFin.HasValue || periodoFin > now));

        public static bool PuedePublicarConEstado(
            TipoEstadoSuscripcion estado,
            DateTime? periodoFin,
            DateTime? pruebaTerminaEn,
            DateTime now) =>
            EsComercialmenteActiva(estado, periodoFin, pruebaTerminaEn, now);

        public static bool EsComercialmenteActiva(Suscripcion suscripcion, DateTime now) =>
            EsComercialmenteActiva(suscripcion.Estado, suscripcion.PeriodoFin, suscripcion.PruebaTerminaEn, now);
    }
}
