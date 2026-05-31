using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiendaVirtual.Comun.Enumeracion
{
    public enum TipoProducto
    {
        [Description("FISICO")] Fisico = 1, 
        [Description("PATRON")] Patron = 2  
    }
}
