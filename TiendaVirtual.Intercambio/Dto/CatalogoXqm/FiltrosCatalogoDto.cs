using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiendaVirtual.Intercambio.Dto.CatalogoXqm
{
    public class FiltrosCatalogoDto
    {
        public int Pagina { get; set; } = 1;
        public int TamanioPagina { get; set; } = 12;

        public int? CategoriaId { get; set; }
        public int? VendedorId { get; set; }
        public string? Busqueda { get; set; }
        public decimal? PrecioMin { get; set; }
        public decimal? PrecioMax { get; set; }
        public int? TipoProducto { get; set; }    // 1=FISICO, 2=PATRON
        public bool? SoloConOferta { get; set; }

        /// <summary>
        /// "precio_asc", "precio_desc", "mas_vendidos",
        /// "mejor_calificados", "novedades" (default)
        /// </summary>
        public string? OrdenarPor { get; set; }
    }
}
