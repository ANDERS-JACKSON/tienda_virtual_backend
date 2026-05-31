using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiendaVirtual.Intercambio
{
    public class ResultadoOperacion<T>
    {
        public bool Exito { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public T? Datos { get; set; } = default!;

        public static ResultadoOperacion<T> SetExito(T datos) =>
            new ResultadoOperacion<T>
            {
                Exito = true,
                Mensaje = string.Empty,
                Datos = datos
            };

        public static ResultadoOperacion<T> SetError(string mensaje) =>
            new ResultadoOperacion<T>
            {
                Exito = false,
                Mensaje = mensaje,
                Datos = default!
            };
    }
}
