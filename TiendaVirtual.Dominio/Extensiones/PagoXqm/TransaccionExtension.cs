using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Modelo.PagoXqm;
using TiendaVirtual.Intercambio.Dto.PagoXqm;
using TiendaVirtual.Intercambio.Dto.Sistema;

namespace TiendaVirtual.Dominio.Extensiones.PagoXqm
{
    public static class TransaccionExtension
    {
        public static Transaccion ToEntidad(this TransaccionDto dto)
        {
            if (dto == null)
                return null!;

            var transaccion = new Transaccion();

            transaccion.TransaccionId = dto.TransaccionId;
            transaccion.OrdenId = dto.OrdenId;
            transaccion.SuscripcionId = dto.SuscripcionId;
            transaccion.UsuarioId = dto.UsuarioId;
            transaccion.Proveedor = dto.Proveedor.Normalizar();
            transaccion.TransaccionProveedorId = dto.TransaccionProveedorId?.Normalizar_null();
            transaccion.Tipo = (TipoTransaccion)dto.Tipo.Id;
            transaccion.Monto = dto.Monto;
            transaccion.Estado = (TipoEstadoTransaccion)dto.Estado.Id;
            transaccion.MetodoPago = dto.MetodoPago?.Normalizar_null();
            transaccion.RespuestaProveedor = dto.RespuestaProveedor?.Normalizar_null();
            transaccion.Fecha = dto.Fecha;

            return transaccion;
        }

        public static TransaccionDto ToDto(this Transaccion entidad)
        {
            if (entidad == null)
                return null!;

            var dto = new TransaccionDto();

            dto.TransaccionId = entidad.TransaccionId;
            dto.OrdenId = entidad.OrdenId;
            dto.SuscripcionId = entidad.SuscripcionId;
            dto.UsuarioId = entidad.UsuarioId;
            dto.Proveedor = entidad.Proveedor;
            dto.TransaccionProveedorId = entidad.TransaccionProveedorId;
            dto.Tipo = new EnumeracionDto
            {
                Id = (int)entidad.Tipo,
                Nombre = entidad.Tipo.GetDescription()
            };
            dto.Monto = entidad.Monto;
            dto.Estado = new EnumeracionDto
            {
                Id = (int)entidad.Estado,
                Nombre = entidad.Estado.GetDescription()
            };
            dto.MetodoPago = entidad.MetodoPago;
            dto.RespuestaProveedor = entidad.RespuestaProveedor;
            dto.Fecha = entidad.Fecha;

            return dto;
        }
    }
}
