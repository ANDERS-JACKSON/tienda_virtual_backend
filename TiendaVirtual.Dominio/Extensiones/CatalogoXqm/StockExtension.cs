using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Dominio.Modelo.CatalogoXqm;
using TiendaVirtual.Intercambio.Dto.CatalogoXqm;

namespace TiendaVirtual.Dominio.Extensiones.CatalogoXqm
{
    public static class StockExtension
    {
        public static Stock ToEntidad(this StockDto dto)
        {
            if (dto == null)
                return null!;

            var stock = new Stock();

            stock.VarianteId = dto.VarianteId;
            stock.CantidadDisponible = dto.CantidadDisponible;
            stock.CantidadReservada = dto.CantidadReservada;
            stock.UmbralStockBajo = dto.UmbralStockBajo;

            return stock;
        }

        public static StockDto ToDto(this Stock entidad)
        {
            if (entidad == null)
                return null!;

            var dto = new StockDto();

            dto.VarianteId = entidad.VarianteId;
            dto.CantidadDisponible = entidad.CantidadDisponible;
            dto.CantidadReservada = entidad.CantidadReservada;
            dto.UmbralStockBajo = entidad.UmbralStockBajo;

            return dto;
        }
    }
}
