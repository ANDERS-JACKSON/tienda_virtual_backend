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
    public static class RetiroExtension
    {
        public static Retiro ToEntidad(this RetiroDto dto)
        {
            if (dto == null)
                return null!;

            var retiro = new Retiro();

            retiro.RetiroId = dto.RetiroId;
            retiro.VendedorId = dto.VendedorId;
            retiro.CuentaId = dto.CuentaId;
            retiro.Monto = dto.Monto;
            retiro.Estado = (TipoEstadoRetiro)dto.Estado.Id;
            retiro.ProcesadoPor = dto.ProcesadoPor;
            retiro.ReferenciaTransferencia = dto.ReferenciaTransferencia?.Normalizar_null();
            retiro.FechaSolicitud = dto.FechaSolicitud;
            retiro.FechaCompletado = dto.FechaCompletado;

            return retiro;
        }

        public static RetiroDto ToDto(this Retiro entidad)
        {
            if (entidad == null)
                return null!;

            var dto = new RetiroDto();

            dto.RetiroId = entidad.RetiroId;
            dto.VendedorId = entidad.VendedorId;
            dto.CuentaId = entidad.CuentaId;
            dto.Monto = entidad.Monto;
            dto.Estado = new EnumeracionDto
            {
                Id = (int)entidad.Estado,
                Nombre = entidad.Estado.GetDescription()
            };
            dto.ProcesadoPor = entidad.ProcesadoPor;
            dto.ReferenciaTransferencia = entidad.ReferenciaTransferencia;
            dto.FechaSolicitud = entidad.FechaSolicitud;
            dto.FechaCompletado = entidad.FechaCompletado;

            return dto;
        }
    }
}
