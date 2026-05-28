using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiendaVirtual.Comun.Enumeracion
{
    public enum TipoEstadoProducto
    {
        [Description("BORRADOR")] 
         Borrador = 1,

        [Description("ACTIVO")] 
         Activo = 2,

        [Description("PAUSADO")] 
         Pausado = 3
    }
}
