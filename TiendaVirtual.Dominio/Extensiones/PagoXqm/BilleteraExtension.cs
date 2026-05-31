using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Modelo.PagoXqm;
using TiendaVirtual.Intercambio.Dto.PagoXqm;

namespace TiendaVirtual.Dominio.Extensiones.PagoXqm
{
    public static class BilleteraExtension
    {
        public static Billetera ToEntidad(this BilleteraDto dto)
        {
            if (dto == null)
                return null!;

            var billetera = new Billetera();

            billetera.VendedorId = dto.VendedorId;
            billetera.SaldoDisponible = dto.SaldoDisponible;
            billetera.SaldoPendiente = dto.SaldoPendiente;
            billetera.TotalGanado = dto.TotalGanado;
            billetera.TotalRetirado = dto.TotalRetirado;

            return billetera;
        }

        public static BilleteraDto ToDto(this Billetera entidad)
        {
            if (entidad == null)
                return null!;

            var dto = new BilleteraDto();

            dto.VendedorId = entidad.VendedorId;
            dto.SaldoDisponible = entidad.SaldoDisponible;
            dto.SaldoPendiente = entidad.SaldoPendiente;
            dto.TotalGanado = entidad.TotalGanado;
            dto.TotalRetirado = entidad.TotalRetirado;

            return dto;
        }
    }
}
