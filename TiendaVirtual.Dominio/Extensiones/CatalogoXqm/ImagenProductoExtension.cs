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
        public static ImagenProductoDto ToDto(this ImagenProducto i)
        {
            if (i == null) return null!;
            return new ImagenProductoDto
            {
                ImagenId = i.ImagenId,
                ProductoId = i.ProductoId,
                Url = i.Url,
                TextoAlt = i.TextoAlt,
                Orden = i.Orden,
                EsPrincipal = i.EsPrincipal
            };
        }
    }
}
