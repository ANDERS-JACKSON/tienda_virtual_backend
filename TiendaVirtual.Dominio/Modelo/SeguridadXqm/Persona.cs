using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Utilidad;
using TiendaVirtual.Intercambio;

namespace TiendaVirtual.Dominio.Modelo.SeguridadXqm
{
    public class Persona
    {
        public int PersonaId { get; set; }

        [EnumValorValido]
        public TipoDocumentoIdentidad TipoDocumento { get; set; }

        [Required]
        public string NumeroDocumento { get; set; } = null!;

        public string? ApellidoPaterno { get; set; }
        public string? ApellidoMaterno { get; set; }

        [Required]
        public string Nombres { get; set; } = null!;

        public TipoSexo? Sexo { get; set; }
        public DateOnly? FechaNacimiento { get; set; }
        public string? Telefono { get; set; }
        public string? CorreoElectronico { get; set; }

        // Relaciones
        public virtual ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
        public virtual ICollection<Direccion> Direcciones { get; set; } = new List<Direccion>();

        public ResultadoOperacion<bool> Validar()
        {
            var respuesta = EntidadValidador.ValidarCamposRequeridos(this);
            if (respuesta.Exito)
            {
                if (string.IsNullOrWhiteSpace(ApellidoPaterno))
                    return ResultadoOperacion<bool>.SetError("El apellido paterno es obligatorio");
                if (string.IsNullOrWhiteSpace(ApellidoMaterno))
                    return ResultadoOperacion<bool>.SetError("El apellido materno es obligatorio");

                switch (TipoDocumento)
                {
                    case TipoDocumentoIdentidad.DNI:
                        if (NumeroDocumento.Length != 8)
                            return ResultadoOperacion<bool>.SetError("El número de documento debe tener 8 dígitos");
                        break;
                    case TipoDocumentoIdentidad.CarnetExtranjeria:
                        if (NumeroDocumento.Length != 12)
                            return ResultadoOperacion<bool>.SetError("El número de documento debe tener 12 dígitos");
                        break;
                    case TipoDocumentoIdentidad.Pasaporte:
                        if (NumeroDocumento.Length != 9)
                            return ResultadoOperacion<bool>.SetError("El número de documento debe tener 9 dígitos");
                        break;
                }

                if (FechaNacimiento.HasValue && FechaNacimiento > DateOnly.FromDateTime(DateTime.Now))
                    return ResultadoOperacion<bool>.SetError("La fecha de nacimiento no puede ser mayor a la fecha actual");
            }
            return respuesta;
        }
    }
}
