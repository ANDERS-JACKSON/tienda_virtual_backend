using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Dominio.Modelo.VentaXqm;
using TiendaVirtual.Intercambio.Dto.VentaXqm;

namespace TiendaVirtual.Dominio.Extensiones.VentaXqm
{
    public static class EnvioExtension
    {
        public static Envio ToEntidad(this EnvioDto dto)
        {
            if (dto == null)
                return null!;

            var envio = new Envio();

            envio.EnvioId = dto.EnvioId;
            envio.SubordenId = dto.SubordenId;
            envio.Transportista = dto.Transportista.Normalizar();
            envio.NumeroSeguimiento = dto.NumeroSeguimiento?.Normalizar_null();
            envio.ComprobanteUrl = dto.ComprobanteUrl?.Normalizar_null();
            envio.MontoComprobante = dto.MontoComprobante;
            envio.FechaEntregaReal = dto.FechaEntregaReal;

            return envio;
        }

        public static EnvioDto ToDto(this Envio entidad)
        {
            if (entidad == null)
                return null!;

            var dto = new EnvioDto();

            dto.EnvioId = entidad.EnvioId;
            dto.SubordenId = entidad.SubordenId;
            dto.Transportista = entidad.Transportista;
            dto.NumeroSeguimiento = entidad.NumeroSeguimiento;
            dto.ComprobanteUrl = entidad.ComprobanteUrl;
            dto.MontoComprobante = entidad.MontoComprobante;
            dto.FechaEntregaReal = entidad.FechaEntregaReal;

            return dto;
        }
    }
}
