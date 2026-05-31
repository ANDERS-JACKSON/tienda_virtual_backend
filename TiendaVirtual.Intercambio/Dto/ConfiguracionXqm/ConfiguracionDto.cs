using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiendaVirtual.Intercambio.Dto.ConfiguracionXqm
{
    public class ConfiguracionDto
    {
        public int ConfiguracionId { get; set; }
        public int TokenDuracionMinutos { get; set; }
        public int DiasLiberacionPago { get; set; }
        public decimal ComisionPorDefecto { get; set; }
        public int Anio { get; set; }
        public string AnioNombre { get; set; } = string.Empty;
    }
}
