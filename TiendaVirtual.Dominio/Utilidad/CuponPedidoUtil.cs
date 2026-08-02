using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Modelo.VentaXqm;

namespace TiendaVirtual.Dominio.Utilidad
{
    public static class CuponPedidoUtil
    {
        public static string? ValidarDisponibilidad(CuponPedido cupon, DateTime ahoraUtc)
        {
            if (!cupon.Activo)
                return "Este cupón no está activo.";

            if (cupon.TipoDescuento is not (TipoDescuentoCupon.Porcentaje or TipoDescuentoCupon.MontoFijo))
                return "Este cupón no aplica a pedidos.";

            if (cupon.ValidoHasta.HasValue && cupon.ValidoHasta.Value < ahoraUtc)
                return "Este cupón ya venció.";

            if (cupon.UsosMaximos.HasValue && cupon.UsosRealizados >= cupon.UsosMaximos.Value)
                return "Este cupón ya alcanzó el máximo de usos.";

            return null;
        }

        public static string? ValidarMontoMinimo(CuponPedido cupon, decimal subtotal)
        {
            if (cupon.MontoMinimo.HasValue && subtotal < cupon.MontoMinimo.Value)
                return $"Compra mínima de S/ {cupon.MontoMinimo.Value:N2} para usar este cupón.";
            return null;
        }

        /// <summary>
        /// Calcula el descuento del cupón sobre el subtotal (ya con ofertas de producto).
        /// Nunca supera el subtotal.
        /// </summary>
        public static decimal CalcularDescuento(CuponPedido cupon, decimal subtotal)
        {
            if (subtotal <= 0) return 0m;

            var descuento = cupon.TipoDescuento switch
            {
                TipoDescuentoCupon.Porcentaje =>
                    Math.Round(subtotal * (cupon.ValorDescuento / 100m), 2),
                TipoDescuentoCupon.MontoFijo =>
                    Math.Round(cupon.ValorDescuento, 2),
                _ => 0m
            };

            return Math.Min(Math.Max(0m, descuento), subtotal);
        }
    }
}
