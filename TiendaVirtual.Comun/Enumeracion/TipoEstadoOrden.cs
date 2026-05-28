using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiendaVirtual.Comun.Enumeracion
{
    public enum TipoEstadoOrden
    {
        [Description("PENDIENTE_PAGO")] 
         PendientePago = 1,

        [Description("PAGADA")] 
         Pagada = 2,

        [Description("ENVIADA")] 
         Enviada = 3,

        [Description("ENTREGADA")] 
         Entregada = 4,

        [Description("CANCELADA")] 
         Cancelada = 5
    }
}
