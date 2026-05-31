using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiendaVirtual.Dominio.Modelo.VendedorXqm
{
    public class CuentaBancaria
    {
        public int CuentaId { get; set; }

        [Required]
        public int VendedorId { get; set; }

        [Required]
        public string Banco { get; set; } = null!;

        [Required]
        public string NumeroCuenta { get; set; } = null!;

        public string? Cci { get; set; }

        [Required]
        public string Titular { get; set; } = null!;

        public bool EsPredeterminada { get; set; }

        public virtual Vendedor Vendedor { get; set; } = null!;
    }
}
