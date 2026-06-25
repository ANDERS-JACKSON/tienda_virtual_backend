using TiendaVirtual.Dominio.Servicios.ConfiguracionXqm;
using TiendaVirtual.Dominio.Servicios.ConfiguracionXqm.Implementacion;

namespace TiendaVirtual.Api.Extensiones
{
    public static class RegistroServiciosConfiguracion
    {
        public static IServiceCollection AgregarServiciosConfiguracion(this IServiceCollection services)
        {
            services.AddScoped<IConfiguracionCorreoServicio, ConfiguracionCorreoServicio>();
            services.AddScoped<IAvisoBannerServicio, AvisoBannerServicio>();
            return services;
        }
    }
}
