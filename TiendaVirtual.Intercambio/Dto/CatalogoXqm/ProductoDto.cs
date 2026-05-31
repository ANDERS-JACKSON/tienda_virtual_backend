using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Intercambio.Dto.Sistema;

namespace TiendaVirtual.Intercambio.Dto.CatalogoXqm
{
    public class ProductoDto
    {
        public int ProductoId { get; set; }
        public int VendedorId { get; set; }
        public int CategoriaId { get; set; }
        public string Nombre { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string? Descripcion { get; set; }
        public string? DescripcionCorta { get; set; }
        public string? Material { get; set; }
        public string? Dimensiones { get; set; }
        public bool TieneVariantes { get; set; }
        public decimal? PrecioBase { get; set; }
        public int? DiasElaboracion { get; set; }
        public EnumeracionDto Estado { get; set; } = null!;
        public EnumeracionDto Tipo { get; set; } = null!;
        public string? ArchivoPatronUrl { get; set; }
        public int Vistas { get; set; }
        public int Ventas { get; set; }
        public decimal CalificacionPromedio { get; set; }
        public int TotalResenas { get; set; }
    }
}
