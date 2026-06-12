using TiendaVirtual.Dominio.Servicios.PagoXqm;
using TiendaVirtual.Dominio.Servicios.PagoXqm.Implementacion;

namespace TiendaVirtual.Api.Extensiones
{
    public static class RegistroServiciosPago
    {
        public static IServiceCollection AgregarServiciosPago(this IServiceCollection services)
        {
            services.AddScoped<ITransaccionAdminServicio, TransaccionAdminServicio>();
            return services;
        }
    }
}
