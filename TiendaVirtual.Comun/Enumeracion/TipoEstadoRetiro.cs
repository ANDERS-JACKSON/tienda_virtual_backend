using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiendaVirtual.Comun.Enumeracion
{
    public enum TipoEstadoRetiro
    {
        [Description("SOLICITADO")] 
         Solicitado = 1,

        [Description("COMPLETADO")]
         Completado = 2
    }
}
