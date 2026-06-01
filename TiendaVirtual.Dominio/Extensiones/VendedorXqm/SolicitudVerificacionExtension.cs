using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Modelo.VendedorXqm;
using TiendaVirtual.Intercambio.Dto.Sistema;
using TiendaVirtual.Intercambio.Dto.VendedorXqm;

namespace TiendaVirtual.Dominio.Extensiones.VendedorXqm
{
    public static class SolicitudVerificacionExtension
    {
        public static SolicitudVerificacion ToEntidad(this SolicitudVerificacionDto dto)
        {
            if (dto == null)
                return null!;

            var solicitud = new SolicitudVerificacion();

            solicitud.SolicitudId = dto.SolicitudId;
            solicitud.VendedorId = dto.VendedorId;
            solicitud.Estado = (TipoEstadoSolicitudVerificacion)dto.Estado.Id;
            solicitud.DocumentoFrenteUrl = dto.DocumentoFrenteUrl.Normalizar();
            solicitud.DocumentoReversoUrl = dto.DocumentoReversoUrl?.Normalizar_null();
            solicitud.SelfieDocumentoUrl = dto.SelfieDocumentoUrl?.Normalizar_null();
            solicitud.FotosProductos = dto.FotosProductos?.Normalizar_null();
            solicitud.VerificadorId = dto.VerificadorId;
            solicitud.NotasRevisor = dto.NotasRevisor?.Normalizar_null();
            solicitud.MotivoRechazo = dto.MotivoRechazo?.Normalizar_null();
            solicitud.FechaEnvio = dto.FechaEnvio;
            solicitud.FechaRevision = dto.FechaRevision;

            return solicitud;
        }

        public static SolicitudVerificacionDto ToDto(this SolicitudVerificacion entidad)
        {
            if (entidad == null)
                return null!;

            var dto = new SolicitudVerificacionDto();

            dto.SolicitudId = entidad.SolicitudId;
            dto.VendedorId = entidad.VendedorId;
            dto.NombreTienda = entidad.Vendedor?.NombreTienda ?? string.Empty;
            dto.CorreoVendedor = entidad.Vendedor?.Usuario?.Correo ?? string.Empty;
            dto.Estado = new EnumeracionDto
            {
                Id = (int)entidad.Estado,
                Nombre = entidad.Estado.GetDescription()
            };
            dto.DocumentoFrenteUrl = entidad.DocumentoFrenteUrl;
            dto.DocumentoReversoUrl = entidad.DocumentoReversoUrl;
            dto.SelfieDocumentoUrl = entidad.SelfieDocumentoUrl;
            dto.FotosProductos = entidad.FotosProductos;
            dto.VerificadorId = entidad.VerificadorId;
            dto.NotasRevisor = entidad.NotasRevisor;
            dto.MotivoRechazo = entidad.MotivoRechazo;
            dto.FechaEnvio = entidad.FechaEnvio;
            dto.FechaRevision = entidad.FechaRevision;

            return dto;
        }
    }
}
