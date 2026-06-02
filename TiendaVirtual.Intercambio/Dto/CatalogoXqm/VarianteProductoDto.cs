using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiendaVirtual.Intercambio.Dto.CatalogoXqm
{
    public class VarianteProductoDto
    {
        public int VarianteId { get; set; }
        public int ProductoId { get; set; }
        public string? Sku { get; set; }
        public string? Nombre { get; set; }
        public decimal Precio { get; set; }
        public int? PesoGramos { get; set; }
        public string? Atributos { get; set; }
        public bool Activa { get; set; }
        public int CantidadDisponible { get; set; }
        public int CantidadReservada { get; set; }
        public int UmbralStockBajo { get; set; }
    }
}
