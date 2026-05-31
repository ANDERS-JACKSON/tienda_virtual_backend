using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiendaVirtual.Intercambio.Dto.VendedorXqm
{
    public class CuentaBancariaDto
    {
        public int CuentaId { get; set; }
        public int VendedorId { get; set; }
        public string Banco { get; set; } = null!;
        public string NumeroCuenta { get; set; } = null!;
        public string? Cci { get; set; }
        public string Titular { get; set; } = null!;
        public bool EsPredeterminada { get; set; }
    }
}
