using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Modelo.VendedorXqm;
using TiendaVirtual.Dominio.Servicios.SuscripcionXqm.Implementacion;
using TiendaVirtual.Intercambio.Dto.Sistema;
using TiendaVirtual.Intercambio.Dto.VendedorXqm;

namespace TiendaVirtual.Dominio.Extensiones.VendedorXqm
{
    public static class SuscripcionExtension
    {
        public static SuscripcionDto ToDto(this Suscripcion entidad, string? nombreTienda = null, string? correoVendedor = null)
        {
            if (entidad == null)
                return null!;

            var estado = entidad.Estado;
            var precioEfectivo = entidad.PrecioPersonalizado ?? entidad.Plan.Precio;
            var fechaReferencia = estado == TipoEstadoSuscripcion.EnPrueba
                ? entidad.PruebaTerminaEn
                : entidad.PeriodoFin;
            var diasRestantes = fechaReferencia.HasValue
                ? Math.Max(0, (int)(fechaReferencia.Value - DateTime.UtcNow).TotalDays)
                : 0;

            var now = DateTime.UtcNow;
            var puedePublicar = SuscripcionBeneficiosHelper.EsComercialmenteActiva(entidad, now);

            return new SuscripcionDto
            {
                SuscripcionId = entidad.SuscripcionId,
                VendedorId = entidad.VendedorId,
                Plan = entidad.Plan.ToDto(),
                Estado = new EnumeracionDto
                {
                    Id = (int)estado,
                    Nombre = estado.GetDescription()
                },
                MesesGratisOtorgados = entidad.MesesGratisOtorgados,
                PrecioPersonalizado = entidad.PrecioPersonalizado,
                PrecioEfectivo = precioEfectivo,
                Cupon = entidad.Cupon?.ToDto(),
                PruebaTerminaEn = entidad.PruebaTerminaEn,
                PeriodoInicio = entidad.PeriodoInicio,
                PeriodoFin = entidad.PeriodoFin,
                RequierePago = estado == TipoEstadoSuscripcion.PendientePago,
                PuedePublicar = puedePublicar,
                DiasRestantes = diasRestantes,
                NombreTienda = nombreTienda,
                CorreoVendedor = correoVendedor
            };
        }
    }
}
