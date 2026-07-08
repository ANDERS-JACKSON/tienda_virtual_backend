using System;
using TiendaVirtual.Dominio.Modelo.CatalogoXqm;

namespace TiendaVirtual.Dominio.Utilidad
{
    /// <summary>
    /// Cálculo unificado de precios con oferta.
    ///
    /// Regla de negocio:
    ///  - La oferta se aplica siempre por PORCENTAJE al precio de cada variante.
    ///  - Si la oferta solo tiene precio fijo (sin porcentaje) se usa como fallback
    ///    para productos SIN variantes reales (una sola variante activa).
    ///  - Cuando el producto tiene varias variantes con precios diferentes, el
    ///    porcentaje es la única forma coherente de aplicar el descuento.
    /// </summary>
    public static class PrecioOfertaUtil
    {
        public readonly struct PrecioCalculado
        {
            public decimal PrecioActual { get; init; }
            public decimal? PrecioOriginal { get; init; }
            public decimal? PorcentajeDescuento { get; init; }
            public bool TieneDescuento => PrecioOriginal.HasValue && PrecioOriginal > PrecioActual;
        }

        /// <summary>
        /// Devuelve el precio final para una variante, aplicando la oferta vigente si corresponde.
        /// </summary>
        /// <param name="precioVariante">Precio actual de la variante.</param>
        /// <param name="oferta">Oferta vigente del producto (puede ser null).</param>
        /// <param name="usarPrecioFijoFallback">
        /// True cuando el producto NO tiene variantes reales; permite usar PrecioOferta como precio fijo.
        /// </param>
        public static PrecioCalculado Calcular(
            decimal precioVariante, Oferta? oferta, bool usarPrecioFijoFallback = false)
        {
            if (oferta == null || precioVariante <= 0 || !EsOfertaVigente(oferta))
            {
                return new PrecioCalculado
                {
                    PrecioActual = Math.Max(0, precioVariante),
                    PrecioOriginal = null,
                    PorcentajeDescuento = null
                };
            }

            if (oferta.PorcentajeDescuento is > 0 and <= 100)
            {
                var pct = oferta.PorcentajeDescuento.Value;
                var precioFinal = Math.Round(precioVariante * (1 - pct / 100m), 2, MidpointRounding.AwayFromZero);
                if (precioFinal < 0.01m) precioFinal = 0.01m;
                return new PrecioCalculado
                {
                    PrecioActual = precioFinal,
                    PrecioOriginal = precioVariante,
                    PorcentajeDescuento = pct
                };
            }

            if (usarPrecioFijoFallback && oferta.PrecioOferta is > 0
                && oferta.PrecioOferta.Value < precioVariante)
            {
                var precioFinal = oferta.PrecioOferta.Value;
                var pct = Math.Round((1 - precioFinal / precioVariante) * 100m, 2, MidpointRounding.AwayFromZero);
                return new PrecioCalculado
                {
                    PrecioActual = precioFinal,
                    PrecioOriginal = precioVariante,
                    PorcentajeDescuento = Math.Clamp(pct, 0, 99)
                };
            }

            return new PrecioCalculado
            {
                PrecioActual = precioVariante,
                PrecioOriginal = null,
                PorcentajeDescuento = null
            };
        }

        /// <summary>Chequeo defensivo: activa y dentro del rango de fechas.</summary>
        private static bool EsOfertaVigente(Oferta o)
        {
            if (!o.Activa) return false;
            var now = DateTime.UtcNow;
            return o.FechaInicio <= now && o.FechaFin >= now;
        }
    }
}
