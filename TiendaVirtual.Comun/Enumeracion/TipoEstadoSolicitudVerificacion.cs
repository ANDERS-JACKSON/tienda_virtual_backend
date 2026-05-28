using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiendaVirtual.Comun.Enumeracion
{
    public enum TipoEstadoSolicitudVerificacion
    {
        [Description("ENVIADA")] 
          Enviada = 1,

        [Description("APROBADA")] 
         Aprobada = 2,

        [Description("RECHAZADA")] 
         Rechazada = 3
    }
}
