using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiendaVirtual.Comun.Enumeracion
{
    public enum TipoDocumentoIdentidad
    {
        [Description("DNI")] 
         DNI = 1,

        [Description("CARNET_EXTRANJERIA")] 
         CarnetExtranjeria = 2,

        [Description("PASAPORTE")] 
         Pasaporte = 3
    }
}
