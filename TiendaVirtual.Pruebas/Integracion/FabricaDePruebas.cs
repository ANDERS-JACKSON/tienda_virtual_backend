using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TiendaVirtual.Api;
using TiendaVirtual.Dominio;

namespace TiendaVirtual.Pruebas.Integracion
{
    /// <summary>
    /// Levanta la API real (Program.cs, incluida la FallbackPolicy, el rate limiting
    /// y la validación de configuración de C3) pero con una base de datos InMemory en
    /// vez de PostgreSQL, para poder probar el pipeline HTTP completo sin una BD real.
    /// </summary>
    public class FabricaDePruebas : WebApplicationFactory<Program>
    {
        public readonly string NombreBaseDatos = Guid.NewGuid().ToString();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");

            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Key"] = "clave-de-pruebas-que-tiene-al-menos-32-bytes-de-longitud",
                    ["Jwt:Issuer"] = "pruebas.tiendavirtual",
                    ["Jwt:Audience"] = "pruebas.tiendavirtual.clientes",
                    ["TwoFactor:ClaveCifrado"] = "kF7PXJWnLEm/HVtpUcvbqf2DMRC9YwgZkXxvR3sZ8ck=",
                    // Valor no vacío para pasar la validación de arranque (C3);
                    // el DbContext real se reemplaza por InMemory más abajo.
                    ["ConnectionStrings:DefaultConnection"] = "placeholder-no-se-usa",
                });
            });

            // ConfigureTestServices (a diferencia de ConfigureServices) se garantiza que
            // corre DESPUÉS del AddDbContext(Npgsql) de Program.cs, así que la
            // sustitución por InMemory realmente reemplaza el proveedor y no deja
            // ambos registrados a la vez.
            builder.ConfigureTestServices(services =>
            {
                var descriptores = services
                    .Where(d =>
                        d.ServiceType == typeof(DbContextOptions<TiendaVirtualDbContext>)
                        || d.ServiceType == typeof(DbContextOptions)
                        || (d.ServiceType.FullName?.StartsWith("Microsoft.EntityFrameworkCore") == true
                            && d.ServiceType.IsGenericType
                            && d.ServiceType.GetGenericArguments().Contains(typeof(TiendaVirtualDbContext)))
                        || d.ServiceType == typeof(TiendaVirtualDbContext))
                    .ToList();
                foreach (var descriptor in descriptores)
                    services.Remove(descriptor);

                services.AddDbContext<TiendaVirtualDbContext>(options =>
                    options.UseInMemoryDatabase(NombreBaseDatos));
            });
        }
    }
}
