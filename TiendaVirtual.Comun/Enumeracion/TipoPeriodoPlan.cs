using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiendaVirtual.Comun.Enumeracion
{
    public enum TipoPeriodoPlan
    {
        [Description("MENSUAL")] 
         Mensual = 1,

        [Description("ANUAL")] 
         Anual = 2
    }
}
