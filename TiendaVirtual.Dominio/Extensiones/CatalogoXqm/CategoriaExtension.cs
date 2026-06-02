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
        public static CategoriaDto ToDto(this Categoria c, int totalProductos = 0)
        {
            if (c == null) return null!;
            return new CategoriaDto
            {
                CategoriaId = c.CategoriaId,
                CategoriaPadreId = c.CategoriaPadreId,
                Nombre = c.Nombre,
                Slug = c.Slug,
                Descripcion = c.Descripcion,
                ImagenUrl = c.ImagenUrl,
                Orden = c.Orden,
                Activa = c.Activa,
                TotalProductos = totalProductos
            };
        }

        public static CategoriaArbolDto ToArbolDto(this Categoria c, int totalProductos = 0)
        {
            if (c == null) return null!;
            return new CategoriaArbolDto
            {
                CategoriaId = c.CategoriaId,
                Nombre = c.Nombre,
                Slug = c.Slug,
                ImagenUrl = c.ImagenUrl,
                Orden = c.Orden,
                TotalProductos = totalProductos,
                Subcategorias = new List<CategoriaArbolDto>()
            };
        }
    }
}
