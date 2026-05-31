using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Modelo.VentaXqm;
using TiendaVirtual.Intercambio.Dto.Sistema;
using TiendaVirtual.Intercambio.Dto.VentaXqm;

namespace TiendaVirtual.Dominio.Extensiones.VentaXqm
{
    public static class OrdenExtension
    {
        public static Orden ToEntidad(this OrdenDto dto)
        {
            if (dto == null)
                return null!;

            var orden = new Orden();

            orden.OrdenId = dto.OrdenId;
            orden.NumeroOrden = dto.NumeroOrden.Normalizar();
            orden.ClienteId = dto.ClienteId;
            orden.Subtotal = dto.Subtotal;
            orden.TotalEnvio = dto.TotalEnvio;
            orden.TotalDescuento = dto.TotalDescuento;
            orden.Total = dto.Total;
            orden.CorreoCliente = dto.CorreoCliente.Normalizar();
            orden.TelefonoCliente = dto.TelefonoCliente?.Normalizar_null();
            orden.DireccionEnvio = dto.DireccionEnvio.Normalizar();
            orden.Estado = (TipoEstadoOrden)dto.Estado.Id;
            orden.Fecha = dto.Fecha;

            return orden;
        }

        public static OrdenDto ToDto(this Orden entidad)
        {
            if (entidad == null)
                return null!;

            var dto = new OrdenDto();

            dto.OrdenId = entidad.OrdenId;
            dto.NumeroOrden = entidad.NumeroOrden;
            dto.ClienteId = entidad.ClienteId;
            dto.Subtotal = entidad.Subtotal;
            dto.TotalEnvio = entidad.TotalEnvio;
            dto.TotalDescuento = entidad.TotalDescuento;
            dto.Total = entidad.Total;
            dto.CorreoCliente = entidad.CorreoCliente;
            dto.TelefonoCliente = entidad.TelefonoCliente;
            dto.DireccionEnvio = entidad.DireccionEnvio;
            dto.Estado = new EnumeracionDto
            {
                Id = (int)entidad.Estado,
                Nombre = entidad.Estado.GetDescription()
            };
            dto.Fecha = entidad.Fecha;

            return dto;
        }
    }
}
