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
        [Description("EN_PREPARACION")] 
         EnPreparacion = 2, 
        [Description("EN_CAMINO")] 
         EnCamino = 3,
        [Description("ENTREGADA")] 
         Entregada = 4, 
        [Description("CANCELADA")] 
         Cancelada = 5,
        [Description("EN_DISPUTA")] 
         EnDisputa = 6
    }
}
