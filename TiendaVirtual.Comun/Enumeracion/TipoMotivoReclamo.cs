using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiendaVirtual.Comun.Enumeracion
{
    public enum TipoMotivoReclamo
    {
        [Description("NO_RECIBIDO")] 
        NoRecibido = 1,

        [Description("DAÑADO")] 
        Danado = 2,

        [Description("NO_COINCIDE")] 
        NoCoincide = 3,

        [Description("OTRO")] 
        Otro = 4
    }
}
