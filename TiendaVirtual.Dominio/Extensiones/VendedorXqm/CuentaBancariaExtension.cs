using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Dominio.Modelo.VendedorXqm;
using TiendaVirtual.Intercambio.Dto.VendedorXqm;

namespace TiendaVirtual.Dominio.Extensiones.VendedorXqm
{
    public static class CuentaBancariaExtension
    {
        public static CuentaBancaria ToEntidad(this CuentaBancariaDto dto)
        {
            if (dto == null)
                return null!;

            var cuenta = new CuentaBancaria();

            cuenta.CuentaId = dto.CuentaId;
            cuenta.VendedorId = dto.VendedorId;
            cuenta.Banco = dto.Banco.Normalizar();
            cuenta.NumeroCuenta = dto.NumeroCuenta.Normalizar();
            cuenta.Cci = dto.Cci?.Normalizar_null();
            cuenta.Titular = dto.Titular.Normalizar();
            cuenta.EsPredeterminada = dto.EsPredeterminada;

            return cuenta;
        }

        public static CuentaBancariaDto ToDto(this CuentaBancaria entidad)
        {
            if (entidad == null)
                return null!;

            var dto = new CuentaBancariaDto();

            dto.CuentaId = entidad.CuentaId;
            dto.VendedorId = entidad.VendedorId;
            dto.Banco = entidad.Banco;
            dto.NumeroCuenta = entidad.NumeroCuenta;
            dto.Cci = entidad.Cci;
            dto.Titular = entidad.Titular;
            dto.EsPredeterminada = entidad.EsPredeterminada;

            return dto;
        }
    }
}
