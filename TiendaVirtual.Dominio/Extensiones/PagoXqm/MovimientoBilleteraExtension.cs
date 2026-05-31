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
    public static class MovimientoBilleteraExtension
    {
        public static MovimientoBilletera ToEntidad(this MovimientoBilleteraDto dto)
        {
            if (dto == null)
                return null!;

            var movimiento = new MovimientoBilletera();

            movimiento.MovimientoId = dto.MovimientoId;
            movimiento.VendedorId = dto.VendedorId;
            movimiento.Tipo = (TipoMovimientoBilletera)dto.Tipo.Id;
            movimiento.Monto = dto.Monto;
            movimiento.SaldoResultante = dto.SaldoResultante;
            movimiento.ReferenciaId = dto.ReferenciaId;
            movimiento.Descripcion = dto.Descripcion?.Normalizar_null();
            movimiento.Fecha = dto.Fecha;

            return movimiento;
        }

        public static MovimientoBilleteraDto ToDto(this MovimientoBilletera entidad)
        {
            if (entidad == null)
                return null!;

            var dto = new MovimientoBilleteraDto();

            dto.MovimientoId = entidad.MovimientoId;
            dto.VendedorId = entidad.VendedorId;
            dto.Tipo = new EnumeracionDto
            {
                Id = (int)entidad.Tipo,
                Nombre = entidad.Tipo.GetDescription()
            };
            dto.Monto = entidad.Monto;
            dto.SaldoResultante = entidad.SaldoResultante;
            dto.ReferenciaId = entidad.ReferenciaId;
            dto.Descripcion = entidad.Descripcion;
            dto.Fecha = entidad.Fecha;

            return dto;
        }
    }
}
