using TiendaVirtual.Dominio.Servicios.CatalogoXqm;
using TiendaVirtual.Dominio.Servicios.CatalogoXqm.Implementacion;

namespace TiendaVirtual.Api.Extensiones
{
    public static class RegistroServiciosCatalogo
    {
        public static IServiceCollection AgregarServiciosCatalogo(this IServiceCollection services)
        {
            services.AddScoped<ICategoriaServicio, CategoriaServicio>();
            services.AddScoped<IProductoServicio, ProductoServicio>();
            services.AddScoped<ICatalogoServicio, CatalogoServicio>();
            services.AddScoped<IFavoritoServicio, FavoritoServicio>();
            services.AddScoped<IProductoDestacadoServicio, ProductoDestacadoServicio>();
            return services;
        }
    }
}
