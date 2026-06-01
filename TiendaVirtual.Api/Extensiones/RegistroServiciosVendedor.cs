using TiendaVirtual.Dominio.Servicios.VendedorXqm;
using TiendaVirtual.Dominio.Servicios.VendedorXqm.Implementacion;

namespace TiendaVirtual.Api.Extensiones
{
    public static class RegistroServiciosVendedor
    {
        public static IServiceCollection AgregarServiciosVendedor(this IServiceCollection services)
        {
            services.AddScoped<IVendedorServicio, VendedorServicio>();
            return services;
        }
    }
}
