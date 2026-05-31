using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Modelo.CatalogoXqm;
using TiendaVirtual.Dominio.Modelo.PagoXqm;
using TiendaVirtual.Dominio.Modelo.SeguridadXqm;
using TiendaVirtual.Dominio.Modelo.SoporteXqm;
using TiendaVirtual.Dominio.Modelo.VentaXqm;
using TiendaVirtual.Dominio.Utilidad;
using TiendaVirtual.Intercambio;

namespace TiendaVirtual.Dominio.Modelo.VendedorXqm
{
    public class Vendedor
    {
        public int VendedorId { get; set; }

        [Required]
        public int UsuarioId { get; set; }

        [Required]
        public string NombreTienda { get; set; } = null!;

        [Required]
        public string SlugTienda { get; set; } = null!;

        public string? Biografia { get; set; }
        public string? LogoUrl { get; set; }
        public string? BannerUrl { get; set; }

        [EnumValorValido]
        public TipoEstadoVendedor Estado { get; set; }

        public decimal CalificacionPromedio { get; set; }
        public int TotalVentas { get; set; }
        public int? InvitadoPor { get; set; }

        public string? NumeroYape { get; set; }
        public bool VendePatrones { get; set; }

        // Relaciones
        public virtual Usuario Usuario { get; set; } = null!;
        public virtual Vendedor? InvitadoPorVendedor { get; set; }
        public virtual ICollection<Vendedor> VendedoresInvitados { get; set; } = new List<Vendedor>();
        public virtual ICollection<SolicitudVerificacion> SolicitudesVerificacion { get; set; } = new List<SolicitudVerificacion>();
        public virtual ICollection<CuentaBancaria> CuentasBancarias { get; set; } = new List<CuentaBancaria>();
        public virtual ICollection<Suscripcion> Suscripciones { get; set; } = new List<Suscripcion>();
        public virtual ICollection<Producto> Productos { get; set; } = new List<Producto>();
        public virtual ICollection<Suborden> Subordenes { get; set; } = new List<Suborden>();
        public virtual ICollection<ResenaVendedor> Resenas { get; set; } = new List<ResenaVendedor>();
        public virtual Billetera? Billetera { get; set; }
        public virtual ICollection<MovimientoBilletera> MovimientosBilletera { get; set; } = new List<MovimientoBilletera>();
        public virtual ICollection<Retiro> Retiros { get; set; } = new List<Retiro>();

        public ResultadoOperacion<bool> Validar()
        {
            return EntidadValidador.ValidarCamposRequeridos(this);
        }
    }
}
