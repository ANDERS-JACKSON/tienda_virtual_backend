using TiendaVirtual.Dominio.Servicios.VentaXqm;
using TiendaVirtual.Dominio.Servicios.VentaXqm.Implementacion;

namespace TiendaVirtual.Api.Extensiones
{
    public static class RegistroServiciosVenta
    {
        public static IServiceCollection AgregarServiciosVenta(this IServiceCollection services)
        {
            services.AddScoped<IDireccionServicio, DireccionServicio>();
            services.AddScoped<IMetodoEnvioServicio, MetodoEnvioServicio>();
            services.AddScoped<ICarritoServicio, CarritoServicio>();
            services.AddScoped<IOrdenServicio, OrdenServicio>();
            services.AddScoped<IOrdenPagoServicio, OrdenPagoServicio>();
            return services;
        }
    }
}
