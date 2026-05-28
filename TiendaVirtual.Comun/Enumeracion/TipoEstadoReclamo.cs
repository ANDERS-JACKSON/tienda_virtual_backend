using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiendaVirtual.Comun.Enumeracion
{
    public enum TipoEstadoReclamo
    {
        [Description("ABIERTO")] 
         Abierto = 1,

        [Description("RESUELTO_CLIENTE")] 
         ResueltoCliente = 2,

        [Description("RESUELTO_VENDEDOR")] 
         ResueltoVendedor = 3
    }
}
