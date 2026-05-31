using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiendaVirtual.Dominio.Utilidad
{
    public class EnumValorValidoAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            if (value == null) return false;

            if (value.GetType().IsEnum)
            {
                int valor = (int)value;
                return valor > 0;
            }

            return false;
        }
    }
}
