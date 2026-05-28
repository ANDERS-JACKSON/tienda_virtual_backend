using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiendaVirtual.Comun.Enumeracion
{
    public enum TipoRol
    {
        [Description("ADMIN")] 
         Admin = 1,

        [Description("CLIENTE")] 
         Cliente = 2,

        [Description("VENDEDOR")] 
         Vendedor = 3,

        [Description("VERIFICADOR")] 
         Verificador = 4
    }
}
