using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace TiendaVirtual.Pruebas.Fakes
{
    /// <summary>
    /// Construye un IConfiguration mínimo con los valores que los servicios de
    /// seguridad necesitan (Jwt/TwoFactor), sin tocar appsettings.json ni
    /// user-secrets reales.
    /// </summary>
    public static class ConfiguracionDePrueba
    {
        public static IConfiguration Crear(Dictionary<string, string?>? extra = null)
        {
            var valores = new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "clave-de-pruebas-que-tiene-al-menos-32-bytes-de-longitud",
                ["Jwt:Issuer"] = "pruebas.tiendavirtual",
                ["Jwt:Audience"] = "pruebas.tiendavirtual.clientes",
                ["TwoFactor:ClaveCifrado"] = "kF7PXJWnLEm/HVtpUcvbqf2DMRC9YwgZkXxvR3sZ8ck=",
                ["TwoFactor:Emisor"] = "TiendaVirtualPruebas",
            };

            if (extra != null)
            {
                foreach (var kv in extra)
                    valores[kv.Key] = kv.Value;
            }

            return new ConfigurationBuilder()
                .AddInMemoryCollection(valores)
                .Build();
        }
    }
}
