using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Dominio.Modelo.CatalogoXqm;
using TiendaVirtual.Intercambio.Dto.CatalogoXqm;

namespace TiendaVirtual.Dominio.Extensiones.CatalogoXqm
{
    public static class OfertaExtension
    {
        public static OfertaDto ToDto(this Oferta o)
        {
            if (o == null) return null!;
            var now = DateTime.UtcNow;
            return new OfertaDto
            {
                OfertaId = o.OfertaId,
                ProductoId = o.ProductoId,
                Nombre = o.Nombre,
                PorcentajeDescuento = o.PorcentajeDescuento,
                PrecioOferta = o.PrecioOferta,
                FechaInicio = o.FechaInicio,
                FechaFin = o.FechaFin,
                Activa = o.Activa,
                Vigente = o.Activa && o.FechaInicio <= now && o.FechaFin >= now
            };
        }
    }
}
