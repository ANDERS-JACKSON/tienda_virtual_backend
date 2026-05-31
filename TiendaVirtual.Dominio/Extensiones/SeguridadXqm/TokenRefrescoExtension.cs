using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Dominio.Modelo.SeguridadXqm;
using TiendaVirtual.Intercambio.Dto.SeguridadXqm;

namespace TiendaVirtual.Dominio.Extensiones.SeguridadXqm
{
    public static class TokenRefrescoExtension
    {
        public static TokenRefresco ToEntidad(this TokenRefrescoDto dto)
        {
            if (dto == null)
                return null!;

            var tokenRefresco = new TokenRefresco();

            tokenRefresco.TokenId = dto.TokenId;
            tokenRefresco.UsuarioId = dto.UsuarioId;
            tokenRefresco.TokenHash = dto.TokenHash.Normalizar();
            tokenRefresco.ExpiraEn = dto.ExpiraEn;
            tokenRefresco.Revocado = dto.Revocado;
            tokenRefresco.FechaEmision = dto.FechaEmision;
            tokenRefresco.DireccionIp = dto.DireccionIp?.Normalizar_null();
            tokenRefresco.AgenteUsuario = dto.AgenteUsuario?.Normalizar_null();

            return tokenRefresco;
        }

        public static TokenRefrescoDto ToDto(this TokenRefresco entidad)
        {
            if (entidad == null)
                return null!;

            var dto = new TokenRefrescoDto();

            dto.TokenId = entidad.TokenId;
            dto.UsuarioId = entidad.UsuarioId;
            dto.TokenHash = entidad.TokenHash;
            dto.ExpiraEn = entidad.ExpiraEn;
            dto.Revocado = entidad.Revocado;
            dto.FechaEmision = entidad.FechaEmision;
            dto.DireccionIp = entidad.DireccionIp;
            dto.AgenteUsuario = entidad.AgenteUsuario;

            return dto;
        }
    }
}
