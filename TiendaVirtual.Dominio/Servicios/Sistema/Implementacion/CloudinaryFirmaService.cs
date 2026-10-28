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

        public ResultadoOperacion<FirmaSubidaMediaDto> Firmar(string? proposito, IEnumerable<string> roles)
        {
            if (!Activo)
            {
                return ResultadoOperacion<FirmaSubidaMediaDto>.SetExito(
                    new FirmaSubidaMediaDto { Activo = false });
            }

            var destino = ResolverDestino(proposito);
            if (destino == null)
                return ResultadoOperacion<FirmaSubidaMediaDto>.SetError("Tipo de archivo no reconocido.");

            if (destino.RolesRequeridos.Length > 0
                && !roles.Any(r => destino.RolesRequeridos.Contains(r, StringComparer.OrdinalIgnoreCase)))
            {
                return ResultadoOperacion<FirmaSubidaMediaDto>.SetError(
                    "No tienes permiso para subir este tipo de archivo.");
            }

            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var parametros = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                ["folder"] = destino.Folder,
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
                Folder = destino.Folder,
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

        private Destino? ResolverDestino(string? proposito)
        {
            var clave = (proposito ?? "imagen").Trim().ToLowerInvariant();
            return clave switch
            {
                "imagen" => new Destino(
                    "image",
                    _configuration["Cloudinary:CarpetaImagenes"] ?? "tienda/imagenes",
                    Array.Empty<string>()),
                "pdf" => new Destino(
                    "raw",
                    _configuration["Cloudinary:CarpetaPatrones"] ?? "tienda/patrones",
                    ["VENDEDOR", "ADMIN"]),
                "kyc" => new Destino(
                    "image",
                    _configuration["Cloudinary:CarpetaKyc"] ?? "tienda/kyc",
                    ["VENDEDOR", "ADMIN"]),
                _ => null
            };
        }

        private sealed record Destino(string ResourceType, string Folder, string[] RolesRequeridos);
    }
}
