using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiendaVirtual.Comun.Enumeracion
{
    public enum TipoEstadoTransaccion
    {
        [Description("PENDIENTE")] 
         Pendiente = 1,

        [Description("PAGADO")] 
         Pagado = 2,

        [Description("FALLIDO")] 
         Fallido = 3,

        [Description("REEMBOLSADO")] 
         Reembolsado = 4
    }
}
