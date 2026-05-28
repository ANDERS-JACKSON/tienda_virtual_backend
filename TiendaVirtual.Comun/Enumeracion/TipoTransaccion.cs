using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiendaVirtual.Comun.Enumeracion
{
    public enum TipoTransaccion
    {
        [Description("PAGO_ORDEN")] 
         PagoOrden = 1,

        [Description("PAGO_SUSCRIPCION")] 
         PagoSuscripcion = 2
    }
}
