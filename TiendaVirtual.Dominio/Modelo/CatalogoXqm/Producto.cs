using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Modelo.SoporteXqm;
using TiendaVirtual.Dominio.Modelo.VendedorXqm;
using TiendaVirtual.Dominio.Utilidad;
using TiendaVirtual.Intercambio;

namespace TiendaVirtual.Dominio.Modelo.CatalogoXqm
{
    public class Producto
    {
        public int ProductoId { get; set; }

        [Required]
        public int VendedorId { get; set; }

        [Required]
        public int CategoriaId { get; set; }

        [Required]
        public string Nombre { get; set; } = null!;

        [Required]
        public string Slug { get; set; } = null!;

        public string? Descripcion { get; set; }
        public string? DescripcionCorta { get; set; }
        public string? Material { get; set; }
        public string? Dimensiones { get; set; }
        public bool TieneVariantes { get; set; }
        public decimal? PrecioBase { get; set; }
        public int? DiasElaboracion { get; set; }

        [EnumValorValido]
        public TipoEstadoProducto Estado { get; set; }

        [EnumValorValido]
        public TipoProducto Tipo { get; set; }

        public string? ArchivoPatronUrl { get; set; }
        public int Vistas { get; set; }
        public int Ventas { get; set; }
        public decimal CalificacionPromedio { get; set; }
        public int TotalResenas { get; set; }
        public string? MotivoPausaAdmin { get; set; }

        public virtual Vendedor Vendedor { get; set; } = null!;
        public virtual Categoria Categoria { get; set; } = null!;
        public virtual ICollection<VarianteProducto> Variantes { get; set; } = new List<VarianteProducto>();
        public virtual ICollection<ImagenProducto> Imagenes { get; set; } = new List<ImagenProducto>();
        public virtual ICollection<Oferta> Ofertas { get; set; } = new List<Oferta>();
        public virtual ICollection<Favorito> Favoritos { get; set; } = new List<Favorito>();
        public virtual ICollection<ResenaProducto> Resenas { get; set; } = new List<ResenaProducto>();
        public virtual ProductoDestacado? Destacado { get; set; }

        public ResultadoOperacion<bool> Validar()
        {
            var respuesta = EntidadValidador.ValidarCamposRequeridos(this);
            if (respuesta.Exito && Tipo == TipoProducto.Patron && string.IsNullOrWhiteSpace(ArchivoPatronUrl))
                return ResultadoOperacion<bool>.SetError("Un producto de tipo PATRON debe tener el archivo PDF.");
            return respuesta;
        }
    }
}
