using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Dominio.Modelo.CatalogoXqm;
using TiendaVirtual.Intercambio.Dto.CatalogoXqm;

namespace TiendaVirtual.Dominio.Extensiones.CatalogoXqm
{
    public static class FavoritoExtension
    {
        public static Favorito ToEntidad(this FavoritoDto dto)
        {
            if (dto == null)
                return null!;

            var favorito = new Favorito();

            favorito.UsuarioId = dto.UsuarioId;
            favorito.ProductoId = dto.ProductoId;
            favorito.Fecha = dto.Fecha;

            return favorito;
        }

        public static FavoritoDto ToDto(this Favorito entidad)
        {
            if (entidad == null)
                return null!;

            var dto = new FavoritoDto();

            dto.UsuarioId = entidad.UsuarioId;
            dto.ProductoId = entidad.ProductoId;
            dto.Fecha = entidad.Fecha;

            return dto;
        }
    }
}
