using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Dominio.Modelo.ConfiguracionXqm;
using TiendaVirtual.Intercambio.Dto.ConfiguracionXqm;

namespace TiendaVirtual.Dominio.Extensiones.ConfiguracionXqm
{
    public static class ConfiguracionExtension
    {
        public static Configuracion ToEntidad(this ConfiguracionDto dto)
        {
            if (dto == null)
                return null!;

            var configuracion = new Configuracion();

            configuracion.ConfiguracionId = dto.ConfiguracionId;
            configuracion.TokenDuracionMinutos = dto.TokenDuracionMinutos;
            configuracion.DiasLiberacionPago = dto.DiasLiberacionPago;
            configuracion.ComisionPorDefecto = dto.ComisionPorDefecto;
            configuracion.Anio = dto.Anio;
            configuracion.AnioNombre = dto.AnioNombre.Normalizar();

            return configuracion;
        }

        public static ConfiguracionDto ToDto(this Configuracion entidad)
        {
            if (entidad == null)
                return null!;

            var dto = new ConfiguracionDto();

            dto.ConfiguracionId = entidad.ConfiguracionId;
            dto.TokenDuracionMinutos = entidad.TokenDuracionMinutos;
            dto.DiasLiberacionPago = entidad.DiasLiberacionPago;
            dto.ComisionPorDefecto = entidad.ComisionPorDefecto;
            dto.Anio = entidad.Anio;
            dto.AnioNombre = entidad.AnioNombre;

            return dto;
        }
    }
}
