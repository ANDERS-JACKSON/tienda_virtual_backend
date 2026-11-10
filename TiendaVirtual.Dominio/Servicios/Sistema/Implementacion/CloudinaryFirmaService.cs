using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.Sistema;

namespace TiendaVirtual.Dominio.Servicios.Sistema.Implementacion
{
    public class CloudinaryFirmaService : ICloudinaryFirmaService
    {
        private readonly IConfiguration _configuration;

        public CloudinaryFirmaService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public bool Activo =>
            !string.IsNullOrWhiteSpace(_configuration["Cloudinary:CloudName"])
            && !string.IsNullOrWhiteSpace(_configuration["Cloudinary:ApiKey"])
            && !string.IsNullOrWhiteSpace(_configuration["Cloudinary:ApiSecret"]);

        public ResultadoOperacion<FirmaSubidaMediaDto> Firmar(
            string? proposito,
            IEnumerable<string> roles,
            Guid? usuarioId = null,
            int? vendedorId = null)
        {
            var destino = ResolverDestino(proposito);
            if (destino == null)
                return ResultadoOperacion<FirmaSubidaMediaDto>.SetError("Tipo de archivo no reconocido.");

            if (destino.RolesRequeridos.Length > 0
                && !roles.Any(r => destino.RolesRequeridos.Contains(r, StringComparer.OrdinalIgnoreCase)))
            {
                return ResultadoOperacion<FirmaSubidaMediaDto>.SetError(
                    "No tienes permiso para subir este tipo de archivo.");
            }

            var folder = ArmarCarpeta(destino.Clave, usuarioId, vendedorId);

            if (!Activo)
            {
                return ResultadoOperacion<FirmaSubidaMediaDto>.SetExito(
                    new FirmaSubidaMediaDto
                    {
                        Activo = false,
                        Folder = folder,
                        ResourceType = destino.ResourceType
                    });
            }

            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var parametros = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                ["folder"] = folder,
                ["timestamp"] = timestamp.ToString()
            };

            var secret = _configuration["Cloudinary:ApiSecret"]!;
            return ResultadoOperacion<FirmaSubidaMediaDto>.SetExito(new FirmaSubidaMediaDto
            {
                Activo = true,
                CloudName = _configuration["Cloudinary:CloudName"],
                ApiKey = _configuration["Cloudinary:ApiKey"],
                Timestamp = timestamp,
                Signature = FirmarParametros(parametros, secret),
                Folder = folder,
                ResourceType = destino.ResourceType
            });
        }

        /// <summary>
        /// Algoritmo documentado por Cloudinary: SHA-1(parametrosOrdenados + api_secret) en hex.
        /// No firma api_key, cloud_name, resource_type ni file.
        /// </summary>
        public static string FirmarParametros(IReadOnlyDictionary<string, string> parametros, string apiSecret)
        {
            var toSign = string.Join("&", parametros
                .Where(kv => !string.IsNullOrEmpty(kv.Value))
                .OrderBy(kv => kv.Key, StringComparer.Ordinal)
                .Select(kv => $"{kv.Key}={kv.Value}"));

            var hash = SHA1.HashData(Encoding.UTF8.GetBytes(toSign + apiSecret));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        internal string ArmarCarpeta(string clave, Guid? usuarioId, int? vendedorId)
        {
            var raiz = (_configuration["Cloudinary:CarpetaRaiz"] ?? "tienda").Trim().Trim('/');
            if (string.IsNullOrEmpty(raiz))
                raiz = "tienda";

            var vendedor = vendedorId is > 0 ? vendedorId.Value.ToString() : null;
            var cliente = usuarioId is { } uid && uid != Guid.Empty
                ? uid.ToString("N")
                : null;

            return clave switch
            {
                "producto" or "imagen" when vendedor != null => $"{raiz}/vendedores/{vendedor}/productos",
                "logo" when vendedor != null => $"{raiz}/vendedores/{vendedor}/logo",
                "banner" when vendedor != null => $"{raiz}/vendedores/{vendedor}/banner",
                "pdf" when vendedor != null => $"{raiz}/vendedores/{vendedor}/patrones",
                "kyc" when vendedor != null => $"{raiz}/vendedores/{vendedor}/kyc",
                "envio" when vendedor != null => $"{raiz}/vendedores/{vendedor}/envios",
                "reclamo" when vendedor != null => $"{raiz}/vendedores/{vendedor}/reclamos",
                "reclamo" when cliente != null => $"{raiz}/clientes/{cliente}/reclamos",
                "resena" when cliente != null => $"{raiz}/clientes/{cliente}/resenas",
                "carrusel" => $"{raiz}/sitio/carrusel",
                "aviso" => $"{raiz}/sitio/avisos",
                "franja" => $"{raiz}/sitio/franjas",
                "pdf" => $"{raiz}/patrones",
                "kyc" => $"{raiz}/kyc",
                "imagen" when cliente != null => $"{raiz}/clientes/{cliente}/otros",
                _ => $"{raiz}/imagenes"
            };
        }

        private Destino? ResolverDestino(string? proposito)
        {
            var clave = (proposito ?? "imagen").Trim().ToLowerInvariant();
            return clave switch
            {
                "imagen" => new Destino("imagen", "image", Array.Empty<string>()),
                "producto" => new Destino("producto", "image", ["VENDEDOR", "ADMIN"]),
                "logo" => new Destino("logo", "image", ["VENDEDOR", "ADMIN"]),
                "banner" => new Destino("banner", "image", ["VENDEDOR", "ADMIN"]),
                "envio" => new Destino("envio", "image", ["VENDEDOR", "ADMIN"]),
                "pdf" => new Destino("pdf", "raw", ["VENDEDOR", "ADMIN"]),
                "kyc" => new Destino("kyc", "image", ["VENDEDOR", "ADMIN"]),
                "reclamo" => new Destino("reclamo", "image", Array.Empty<string>()),
                "resena" => new Destino("resena", "image", Array.Empty<string>()),
                "carrusel" => new Destino("carrusel", "image", ["ADMIN"]),
                "aviso" => new Destino("aviso", "image", ["ADMIN"]),
                "franja" => new Destino("franja", "image", ["ADMIN"]),
                _ => null
            };
        }

        private sealed record Destino(string Clave, string ResourceType, string[] RolesRequeridos);
    }
}
