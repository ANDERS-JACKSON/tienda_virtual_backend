using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Intercambio.Dto.Sistema;

namespace TiendaVirtual.Intercambio.Dto.CatalogoXqm
{
    /// <summary>DTO ligero para tarjetas del catálogo público.</summary>
    public class ProductoListadoDto
    {
        public int ProductoId { get; set; }
        public string Nombre { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string? DescripcionCorta { get; set; }
        public string? ImagenPrincipalUrl { get; set; }
        public int VendedorId { get; set; }
        public string NombreTienda { get; set; } = null!;
        public string SlugTienda { get; set; } = null!;
        public int CategoriaId { get; set; }
        public string NombreCategoria { get; set; } = null!;
        public decimal PrecioBase { get; set; }
        public decimal? PrecioOferta { get; set; }
        public decimal? PorcentajeDescuento { get; set; }
        public bool TieneOferta { get; set; }
        public EnumeracionDto Tipo { get; set; } = null!;
        public decimal CalificacionPromedio { get; set; }
        public int TotalResenas { get; set; }
        public bool TieneStock { get; set; }
    }
}
