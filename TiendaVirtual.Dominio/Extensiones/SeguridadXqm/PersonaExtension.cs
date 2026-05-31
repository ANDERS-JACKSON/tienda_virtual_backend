using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Modelo.SeguridadXqm;
using TiendaVirtual.Intercambio.Dto.SeguridadXqm;
using TiendaVirtual.Intercambio.Dto.Sistema;

namespace TiendaVirtual.Dominio.Extensiones.SeguridadXqm
{
    public static class PersonaExtension
    {
        public static Persona ToEntidad(this PersonaDto dto)
        {
            if (dto == null)
                return null!;

            var persona = new Persona();

            persona.PersonaId = dto.PersonaId;
            persona.TipoDocumento = (TipoDocumentoIdentidad)dto.TipoDocumento.Id;
            persona.NumeroDocumento = dto.NumeroDocumento.Normalizar();
            persona.ApellidoPaterno = dto.ApellidoPaterno?.Normalizar_null();
            persona.ApellidoMaterno = dto.ApellidoMaterno?.Normalizar_null();
            persona.Nombres = dto.Nombres.Normalizar();
            persona.Sexo = dto.Sexo != null ? (TipoSexo)dto.Sexo.Id : null;
            persona.FechaNacimiento = dto.FechaNacimiento;
            persona.Telefono = dto.Telefono?.Normalizar_null();
            persona.CorreoElectronico = dto.CorreoElectronico?.Normalizar_null();

            return persona;
        }

        public static PersonaDto ToDto(this Persona entidad)
        {
            if (entidad == null)
                return null!;

            var dto = new PersonaDto();

            dto.PersonaId = entidad.PersonaId;
            dto.TipoDocumento = new EnumeracionDto
            {
                Id = (int)entidad.TipoDocumento,
                Nombre = entidad.TipoDocumento.GetDescription()
            };
            dto.NumeroDocumento = entidad.NumeroDocumento;
            dto.ApellidoPaterno = entidad.ApellidoPaterno;
            dto.ApellidoMaterno = entidad.ApellidoMaterno;
            dto.Nombres = entidad.Nombres;
            dto.Sexo = entidad.Sexo.HasValue
                ? new EnumeracionDto
                {
                    Id = (int)entidad.Sexo.Value,
                    Nombre = entidad.Sexo.Value.GetDescription()
                }
                : null;
            dto.FechaNacimiento = entidad.FechaNacimiento;
            dto.Telefono = entidad.Telefono;
            dto.CorreoElectronico = entidad.CorreoElectronico;

            return dto;
        }
    }
}
