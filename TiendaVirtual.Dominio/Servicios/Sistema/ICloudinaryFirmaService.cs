using System.Collections.Generic;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.Sistema;

namespace TiendaVirtual.Dominio.Servicios.Sistema
{
    /// <summary>
    /// Firma HMAC/SHA-1 de Cloudinary (algoritmo oficial, sin SDK).
    /// Si falta ApiSecret, Activo=false y el frontend sigue con el preset unsigned.
    /// </summary>
    public interface ICloudinaryFirmaService
    {
        bool Activo { get; }

        ResultadoOperacion<FirmaSubidaMediaDto> Firmar(string? proposito, IEnumerable<string> roles);
    }
}
