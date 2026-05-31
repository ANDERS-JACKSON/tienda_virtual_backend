using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiendaVirtual.Dominio.Extensiones
{
    public static class StringExtensiones
    {
        public static string Normalizar(this string? input)
        {
            if (input == null)
                return string.Empty;

            return input.Trim().ToUpper();
        }

        public static string? Normalizar_null(this string? input)
        {
            if (input == null)
                return null;

            return input.Trim().ToUpper();
        }
    }
}
