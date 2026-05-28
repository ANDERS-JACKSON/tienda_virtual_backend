using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiendaVirtual.Comun.Enumeracion
{
    public enum TipoEstadoSuborden
    {
        [Description("PENDIENTE")] 
         Pendiente = 1,

        [Description("ENVIADA")] 
         Enviada = 2,

        [Description("ENTREGADA")] 
         Entregada = 3,

        [Description("EN_DISPUTA")] 
         EnDisputa = 4
    }
}
