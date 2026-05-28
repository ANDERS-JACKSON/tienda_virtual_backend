using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiendaVirtual.Comun.Enumeracion
{
    public enum TipoEstadoUsuario
    {
        [Description("ACTIVO")] 
         Activo = 1,

        [Description("SUSPENDIDO")] 
         Suspendido = 2,

        [Description("ELIMINADO")] 
         Eliminado = 3
    }
}
