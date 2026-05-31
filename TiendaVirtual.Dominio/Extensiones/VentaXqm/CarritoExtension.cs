using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Dominio.Modelo.VentaXqm;
using TiendaVirtual.Intercambio.Dto.VentaXqm;

namespace TiendaVirtual.Dominio.Extensiones.VentaXqm
{
    public static class CarritoExtension
    {
        public static Carrito ToEntidad(this CarritoDto dto)
        {
            if (dto == null)
                return null!;

            var carrito = new Carrito();

            carrito.CarritoId = dto.CarritoId;
            carrito.UsuarioId = dto.UsuarioId;

            return carrito;
        }

        public static CarritoDto ToDto(this Carrito entidad)
        {
            if (entidad == null)
                return null!;

            var dto = new CarritoDto();

            dto.CarritoId = entidad.CarritoId;
            dto.UsuarioId = entidad.UsuarioId;

            return dto;
        }
    }
}
