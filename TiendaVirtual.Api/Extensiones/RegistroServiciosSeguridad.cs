using TiendaVirtual.Dominio.Servicios.SeguridadXqm;
using TiendaVirtual.Dominio.Servicios.SeguridadXqm.Implementacion;
using TiendaVirtual.Dominio.Servicios.Sistema;
using TiendaVirtual.Dominio.Servicios.Sistema.Implementacion;

namespace TiendaVirtual.Api.Extensiones
{
    public static class RegistroServiciosSeguridad
    {
        public static IServiceCollection AgregarServiciosSeguridad(this IServiceCollection services)
        {
            services.AddScoped<JwtTokenService>();
            services.AddScoped<ITwoFactorService, TwoFactorService>();
            services.AddScoped<IAutenticacionServicio, AutenticacionServicio>();
            services.AddScoped<IUsuarioAdminServicio, UsuarioAdminServicio>();
            return services;
        }
    }
}
