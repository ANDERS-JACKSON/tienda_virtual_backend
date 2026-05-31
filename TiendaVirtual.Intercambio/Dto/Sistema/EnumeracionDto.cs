using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiendaVirtual.Intercambio.Dto.Sistema
{
    public class EnumeracionDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;

        public EnumeracionDto()
        {
            Id = 0;
            Nombre = string.Empty;
        }

        public EnumeracionDto(int id, string nombre)
        {
            Id = id;
            Nombre = nombre;
        }
    }
}
