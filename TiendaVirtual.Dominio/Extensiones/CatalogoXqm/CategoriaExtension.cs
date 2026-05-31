using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Dominio.Modelo.CatalogoXqm;
using TiendaVirtual.Intercambio.Dto.CatalogoXqm;

namespace TiendaVirtual.Dominio.Extensiones.CatalogoXqm
{
    public static class CategoriaExtension
    {
        public static Categoria ToEntidad(this CategoriaDto dto)
        {
            if (dto == null)
                return null!;

            var categoria = new Categoria();

            categoria.CategoriaId = dto.CategoriaId;
            categoria.CategoriaPadreId = dto.CategoriaPadreId;
            categoria.Nombre = dto.Nombre.Normalizar();
            categoria.Slug = dto.Slug.Normalizar();
            categoria.Descripcion = dto.Descripcion?.Normalizar_null();
            categoria.ImagenUrl = dto.ImagenUrl?.Normalizar_null();
            categoria.Orden = dto.Orden;
            categoria.Activa = dto.Activa;

            return categoria;
        }

        public static CategoriaDto ToDto(this Categoria entidad)
        {
            if (entidad == null)
                return null!;

            var dto = new CategoriaDto();

            dto.CategoriaId = entidad.CategoriaId;
            dto.CategoriaPadreId = entidad.CategoriaPadreId;
            dto.Nombre = entidad.Nombre;
            dto.Slug = entidad.Slug;
            dto.Descripcion = entidad.Descripcion;
            dto.ImagenUrl = entidad.ImagenUrl;
            dto.Orden = entidad.Orden;
            dto.Activa = entidad.Activa;

            return dto;
        }
    }
}
