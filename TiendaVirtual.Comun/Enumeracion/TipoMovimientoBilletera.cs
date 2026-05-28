using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiendaVirtual.Comun.Enumeracion
{
    public enum TipoMovimientoBilletera
    {
        [Description("CREDITO_VENTA")] 
         CreditoVenta = 1,

        [Description("DEBITO_RETIRO")] 
         DebitoRetiro = 2,

        [Description("DEBITO_REEMBOLSO")] 
         DebitoReembolso = 3
    }
}
