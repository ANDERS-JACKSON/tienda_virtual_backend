using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiendaVirtual.Comun.Enumeracion
{
    public enum TipoSexo
    {
        [Description("MASCULINO")] 
         Masculino = 1,

        [Description("FEMENINO")] 
         Femenino = 2
    }
}
