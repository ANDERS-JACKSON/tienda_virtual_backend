using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Modelo.VendedorXqm;
using TiendaVirtual.Intercambio.Dto.Sistema;
using TiendaVirtual.Intercambio.Dto.VendedorXqm;

namespace TiendaVirtual.Dominio.Extensiones.VendedorXqm
{
    public static class VendedorExtension
    {
        public static Vendedor ToEntidad(this VendedorDto dto)
        {
            if (dto == null)
                return null!;

            var vendedor = new Vendedor();

            vendedor.VendedorId = dto.VendedorId;
            vendedor.UsuarioId = dto.UsuarioId;
            vendedor.NombreTienda = dto.NombreTienda.Normalizar();
            vendedor.SlugTienda = dto.SlugTienda.Normalizar();
            vendedor.Biografia = dto.Biografia?.Normalizar_null();
            vendedor.LogoUrl = dto.LogoUrl?.Normalizar_null();
            vendedor.BannerUrl = dto.BannerUrl?.Normalizar_null();
            vendedor.Estado = (TipoEstadoVendedor)dto.Estado.Id;
            vendedor.CalificacionPromedio = dto.CalificacionPromedio;
            vendedor.TotalVentas = dto.TotalVentas;
            vendedor.InvitadoPor = dto.InvitadoPor;
            vendedor.NumeroYape = dto.NumeroYape?.Normalizar_null();
            vendedor.VendePatrones = dto.VendePatrones;

            return vendedor;
        }

        public static VendedorDto ToDto(this Vendedor entidad)
        {
            if (entidad == null)
                return null!;

            var dto = new VendedorDto();

            dto.VendedorId = entidad.VendedorId;
            dto.UsuarioId = entidad.UsuarioId;
            dto.NombreTienda = entidad.NombreTienda;
            dto.SlugTienda = entidad.SlugTienda;
            dto.Biografia = entidad.Biografia;
            dto.LogoUrl = entidad.LogoUrl;
            dto.BannerUrl = entidad.BannerUrl;
            dto.Estado = new EnumeracionDto
            {
                Id = (int)entidad.Estado,
                Nombre = entidad.Estado.GetDescription()
            };
            dto.CalificacionPromedio = entidad.CalificacionPromedio;
            dto.TotalVentas = entidad.TotalVentas;
            dto.InvitadoPor = entidad.InvitadoPor;
            dto.NumeroYape = entidad.NumeroYape;
            dto.VendePatrones = entidad.VendePatrones;

            return dto;
        }
    }
}
