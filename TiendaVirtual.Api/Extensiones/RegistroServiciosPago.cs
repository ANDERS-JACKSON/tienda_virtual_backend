using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Opciones;
using TiendaVirtual.Dominio.Servicios.PagoXqm;
using TiendaVirtual.Dominio.Servicios.PagoXqm.Implementacion;

namespace TiendaVirtual.Api.Extensiones
{
    public static class RegistroServiciosPago
    {
        public static IServiceCollection AgregarServiciosPago(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<PagoProveedorOpciones>(configuration.GetSection(PagoProveedorOpciones.Seccion));
            services.Configure<MercadoPagoOpciones>(configuration.GetSection(MercadoPagoOpciones.Seccion));
            services.Configure<IzipayOpciones>(configuration.GetSection(IzipayOpciones.Seccion));

            services.AddKeyedScoped<IProveedorPagoServicio, IzipayProveedorPagoServicio>(
                CodigoProveedorPago.Izipay);
            services.AddKeyedScoped<IProveedorPagoServicio, MercadoPagoProveedorPagoServicio>(
                CodigoProveedorPago.MercadoPago);

            services.AddScoped<IProveedorPagoFactory, ProveedorPagoFactory>();
            services.AddScoped<ITransaccionAdminServicio, TransaccionAdminServicio>();

            return services;
        }
    }
}
