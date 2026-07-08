using TiendaVirtual.Dominio.Servicios.ReporteXqm;
using TiendaVirtual.Dominio.Servicios.ReporteXqm.Implementacion;

namespace TiendaVirtual.Api.Extensiones
{
    public static class RegistroServiciosReporte
    {
        public static IServiceCollection AgregarServiciosReporte(this IServiceCollection services)
        {
            services.AddScoped<IReporteAdminServicio, ReporteAdminServicio>();
            return services;
        }
    }
}
