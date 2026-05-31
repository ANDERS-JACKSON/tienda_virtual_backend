using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Intercambio;

namespace TiendaVirtual.Dominio.Utilidad
{
    public static class EntidadValidador
    {
        public static List<ValidationResult> ValidarCamposRequeridosCore(object entidad)
        {
            var resultados = new List<ValidationResult>();
            var contexto = new ValidationContext(entidad, serviceProvider: null, items: null);
            Validator.TryValidateObject(entidad, contexto, resultados, validateAllProperties: true);
            return resultados;
        }

        public static ResultadoOperacion<bool> ValidarCamposRequeridos(object entidad)
        {
            var errores = EntidadValidador.ValidarCamposRequeridosCore(entidad);
            if (errores.Any())
            {
                // Concatenar errores con salto de línea
                var mensajeErrores = string.Join("\r\n", errores.Select(e =>
                {
                    var campo = string.Join(", ", e.MemberNames);
                    var traducido = TraductorErroresValidacion.Traducir(e.ErrorMessage ?? "", campo);
                    return $"Error en {campo}: {traducido}";
                }));

                //retornando los errores
                return ResultadoOperacion<bool>.SetError(mensajeErrores);
            }

            //si no existen errores retornar exito
            return ResultadoOperacion<bool>.SetExito(true);
        }

    }
}
