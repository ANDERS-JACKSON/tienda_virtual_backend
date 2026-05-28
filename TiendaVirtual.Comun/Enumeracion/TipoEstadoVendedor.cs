using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiendaVirtual.Comun.Enumeracion
{
    public enum TipoEstadoVendedor
    {
        [Description("PENDIENTE_VERIFICACION")] 
         PendienteVerificacion = 1,

        [Description("ACTIVO")] 
         Activo = 2,

        [Description("SUSPENDIDO")] 
         Suspendido = 3,

        [Description("RECHAZADO")] 
         Rechazado = 4
    }
}
