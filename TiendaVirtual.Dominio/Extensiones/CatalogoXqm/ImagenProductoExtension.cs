using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Dominio.Modelo.CatalogoXqm;
using TiendaVirtual.Intercambio.Dto.CatalogoXqm;

namespace TiendaVirtual.Dominio.Extensiones.CatalogoXqm
{
    public static class ImagenProductoExtension
    {
        public static ImagenProducto ToEntidad(this ImagenProductoDto dto)
        {
            if (dto == null)
                return null!;

            var imagen = new ImagenProducto();

            imagen.ImagenId = dto.ImagenId;
            imagen.ProductoId = dto.ProductoId;
            imagen.Url = dto.Url.Normalizar();
            imagen.TextoAlt = dto.TextoAlt?.Normalizar_null();
            imagen.Orden = dto.Orden;
            imagen.EsPrincipal = dto.EsPrincipal;

            return imagen;
        }

        public static ImagenProductoDto ToDto(this ImagenProducto entidad)
        {
            if (entidad == null)
                return null!;

            var dto = new ImagenProductoDto();

            dto.ImagenId = entidad.ImagenId;
            dto.ProductoId = entidad.ProductoId;
            dto.Url = entidad.Url;
            dto.TextoAlt = entidad.TextoAlt;
            dto.Orden = entidad.Orden;
            dto.EsPrincipal = entidad.EsPrincipal;

            return dto;
        }
    }
}
