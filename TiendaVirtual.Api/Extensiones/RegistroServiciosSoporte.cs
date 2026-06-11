using Microsoft.Extensions.DependencyInjection;
using TiendaVirtual.Dominio.Servicios.SoporteXqm;
using TiendaVirtual.Dominio.Servicios.SoporteXqm.Implementacion;

namespace TiendaVirtual.Api.Extensiones
{
    public static class RegistroServiciosSoporte
    {
        public static IServiceCollection AgregarServiciosSoporte(this IServiceCollection services)
        {
            services.AddSingleton<IEmailServicio, EmailServicio>();
            services.AddScoped<INotificacionServicio, NotificacionServicio>();
            services.AddScoped<IReclamoServicio, ReclamoServicio>();
            services.AddScoped<IResenaServicio, ResenaServicio>();
            return services;
        }
    }
}
