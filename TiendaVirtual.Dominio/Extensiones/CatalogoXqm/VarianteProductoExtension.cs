using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Dominio.Modelo.CatalogoXqm;
using TiendaVirtual.Intercambio.Dto.CatalogoXqm;

namespace TiendaVirtual.Dominio.Extensiones.CatalogoXqm
{
    public static class VarianteProductoExtension
    {
        public static VarianteProductoDto ToDto(this VarianteProducto v)
        {
            if (v == null) return null!;
            return new VarianteProductoDto
            {
                VarianteId = v.VarianteId,
                ProductoId = v.ProductoId,
                Sku = v.Sku,
                Nombre = v.Nombre,
                Precio = v.Precio,
                PesoGramos = v.PesoGramos,
                Atributos = v.Atributos,
                Activa = v.Activa,
                CantidadDisponible = v.Stock?.CantidadDisponible ?? 0,
                CantidadReservada = v.Stock?.CantidadReservada ?? 0,
                UmbralStockBajo = v.Stock?.UmbralStockBajo ?? 5
            };
        }
    }
}
