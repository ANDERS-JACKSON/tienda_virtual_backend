using TiendaVirtual.Dominio.Servicios.SuscripcionXqm;
using TiendaVirtual.Dominio.Servicios.SuscripcionXqm.Implementacion;

namespace TiendaVirtual.Api.Extensiones
{
    public static class RegistroServiciosSuscripcion
    {
        public static IServiceCollection AgregarServiciosSuscripcion(this IServiceCollection services)
        {
            services.AddScoped<IPlanServicio, PlanServicio>();
            services.AddScoped<ICuponServicio, CuponServicio>();
            services.AddScoped<ISuscripcionServicio, SuscripcionServicio>();
            services.AddScoped<ISuscripcionPagoServicio, SuscripcionPagoServicio>();
            return services;
        }
    }
}
