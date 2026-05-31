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
        public static Oferta ToEntidad(this OfertaDto dto)
        {
            if (dto == null)
                return null!;

            var oferta = new Oferta();

            oferta.OfertaId = dto.OfertaId;
            oferta.ProductoId = dto.ProductoId;
            oferta.Nombre = dto.Nombre?.Normalizar_null();
            oferta.PorcentajeDescuento = dto.PorcentajeDescuento;
            oferta.PrecioOferta = dto.PrecioOferta;
            oferta.FechaInicio = dto.FechaInicio;
            oferta.FechaFin = dto.FechaFin;
            oferta.Activa = dto.Activa;

            return oferta;
        }

        public static OfertaDto ToDto(this Oferta entidad)
        {
            if (entidad == null)
                return null!;

            var dto = new OfertaDto();

            dto.OfertaId = entidad.OfertaId;
            dto.ProductoId = entidad.ProductoId;
            dto.Nombre = entidad.Nombre;
            dto.PorcentajeDescuento = entidad.PorcentajeDescuento;
            dto.PrecioOferta = entidad.PrecioOferta;
            dto.FechaInicio = entidad.FechaInicio;
            dto.FechaFin = entidad.FechaFin;
            dto.Activa = entidad.Activa;

            return dto;
        }
    }
}
