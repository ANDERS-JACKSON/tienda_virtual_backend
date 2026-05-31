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
    public static class SubordenExtension
    {
        public static Suborden ToEntidad(this SubordenDto dto)
        {
            if (dto == null)
                return null!;

            var suborden = new Suborden();

            suborden.SubordenId = dto.SubordenId;
            suborden.OrdenId = dto.OrdenId;
            suborden.VendedorId = dto.VendedorId;
            suborden.NumeroSuborden = dto.NumeroSuborden.Normalizar();
            suborden.Subtotal = dto.Subtotal;
            suborden.MontoEnvio = dto.MontoEnvio;
            suborden.MontoComision = dto.MontoComision;
            suborden.MontoVendedor = dto.MontoVendedor;
            suborden.Estado = (TipoEstadoSuborden)dto.Estado.Id;
            suborden.FechaEnvio = dto.FechaEnvio;
            suborden.FechaEntrega = dto.FechaEntrega;

            return suborden;
        }

        public static SubordenDto ToDto(this Suborden entidad)
        {
            if (entidad == null)
                return null!;

            var dto = new SubordenDto();

            dto.SubordenId = entidad.SubordenId;
            dto.OrdenId = entidad.OrdenId;
            dto.VendedorId = entidad.VendedorId;
            dto.NumeroSuborden = entidad.NumeroSuborden;
            dto.Subtotal = entidad.Subtotal;
            dto.MontoEnvio = entidad.MontoEnvio;
            dto.MontoComision = entidad.MontoComision;
            dto.MontoVendedor = entidad.MontoVendedor;
            dto.Estado = new EnumeracionDto
            {
                Id = (int)entidad.Estado,
                Nombre = entidad.Estado.GetDescription()
            };
            dto.FechaEnvio = entidad.FechaEnvio;
            dto.FechaEntrega = entidad.FechaEntrega;

            return dto;
        }
    }
}
