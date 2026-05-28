using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiendaVirtual.Comun.Enumeracion
{
    public enum TipoDescuentoCupon
    {
        [Description("PORCENTAJE")] 
         Porcentaje = 1,

        [Description("MESES_GRATIS")] 
         MesesGratis = 2
    }
}
