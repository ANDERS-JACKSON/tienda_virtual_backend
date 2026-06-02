using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Intercambio.Dto.Sistema;
using TiendaVirtual.Intercambio.Dto.VendedorXqm;

namespace TiendaVirtual.Intercambio.Dto.CatalogoXqm
{
    /// <summary>DTO de detalle público del producto.</summary>
    public class ProductoDetalleDto
    {
        public int ProductoId { get; set; }
        public string Nombre { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string? Descripcion { get; set; }
        public string? DescripcionCorta { get; set; }
        public string? Material { get; set; }
        public string? Dimensiones { get; set; }
        public bool TieneVariantes { get; set; }
        public decimal? PrecioBase { get; set; }
        public int? DiasElaboracion { get; set; }
        public EnumeracionDto Tipo { get; set; } = null!;
        public int Vistas { get; set; }
        public int Ventas { get; set; }
        public decimal CalificacionPromedio { get; set; }
        public int TotalResenas { get; set; }

        public CategoriaDto Categoria { get; set; } = null!;
        public TiendaPublicaDto Vendedor { get; set; } = null!;
        public List<VarianteProductoDto> Variantes { get; set; } = new();
        public List<ImagenProductoDto> Imagenes { get; set; } = new();
        public OfertaDto? OfertaVigente { get; set; }

        // Calculados para el front
        public decimal PrecioActual { get; set; }   // precio_oferta si hay, sino precio_base
        public decimal? PrecioAnterior { get; set; } // precio_base si hay oferta, sino null
        public bool TieneStock { get; set; }
    }
}
