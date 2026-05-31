using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Dominio.Modelo.SeguridadXqm;
using TiendaVirtual.Intercambio.Dto.SeguridadXqm;

namespace TiendaVirtual.Dominio.Extensiones.SeguridadXqm
{
    public static class DireccionExtension
    {
        public static Direccion ToEntidad(this DireccionDto dto)
        {
            if (dto == null)
                return null!;

            var direccion = new Direccion();

            direccion.DireccionId = dto.DireccionId;
            direccion.PersonaId = dto.PersonaId;
            direccion.Etiqueta = dto.Etiqueta?.Normalizar_null();
            direccion.NombreReceptor = dto.NombreReceptor.Normalizar();
            direccion.Telefono = dto.Telefono?.Normalizar_null();
            direccion.Departamento = dto.Departamento.Normalizar();
            direccion.Provincia = dto.Provincia.Normalizar();
            direccion.Distrito = dto.Distrito.Normalizar();
            direccion.DireccionLinea = dto.DireccionLinea.Normalizar();
            direccion.Referencia = dto.Referencia?.Normalizar_null();
            direccion.EsPredeterminada = dto.EsPredeterminada;

            return direccion;
        }

        public static DireccionDto ToDto(this Direccion entidad)
        {
            if (entidad == null)
                return null!;

            var dto = new DireccionDto();

            dto.DireccionId = entidad.DireccionId;
            dto.PersonaId = entidad.PersonaId;
            dto.Etiqueta = entidad.Etiqueta;
            dto.NombreReceptor = entidad.NombreReceptor;
            dto.Telefono = entidad.Telefono;
            dto.Departamento = entidad.Departamento;
            dto.Provincia = entidad.Provincia;
            dto.Distrito = entidad.Distrito;
            dto.DireccionLinea = entidad.DireccionLinea;
            dto.Referencia = entidad.Referencia;
            dto.EsPredeterminada = entidad.EsPredeterminada;

            return dto;
        }
    }
}
