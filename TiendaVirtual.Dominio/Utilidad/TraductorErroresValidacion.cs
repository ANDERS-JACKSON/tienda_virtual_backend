using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiendaVirtual.Dominio.Utilidad
{
    public static class TraductorErroresValidacion
    {
        private static readonly Dictionary<string, string> Traducciones = new()
    {
        { "The field {0} is required.", "El campo {0} es obligatorio." },
        { "The {0} field is required.", "El campo {0} es obligatorio." },
        { "The field must be a string or array type with a maximum length of '{1}'.", "El campo debe tener una longitud máxima de {1} caracteres." },
        { "The field {0} must be a string with a maximum length of {1}.", "El campo {0} debe tener como máximo {1} caracteres." },
        { "The value '{0}' is not valid for {1}.", "El valor '{0}' no es válido para {1}." }
    };

        public static string Traducir(string mensajeOriginal, string? campo = null)
        {
            foreach (var par in Traducciones)
            {
                if (mensajeOriginal.StartsWith(par.Key.Split("{")[0]))
                {
                    return string.Format(par.Value, campo ?? "", "");
                }
            }

            return mensajeOriginal;
        }
    }
}
