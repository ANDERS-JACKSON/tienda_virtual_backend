using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiendaVirtual.Comun.Enumeracion
{
    public enum TipoEstadoSuscripcion
    {
        [Description("EN_PRUEBA")] 
         EnPrueba = 1,

        [Description("ACTIVA")] 
         Activa = 2,

        [Description("VENCIDA")] 
         Vencida = 3,

        [Description("CANCELADA")] 
         Cancelada = 4
    }
}
