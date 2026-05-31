using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Modelo.CatalogoXqm;
using TiendaVirtual.Dominio.Modelo.PagoXqm;
using TiendaVirtual.Dominio.Modelo.SoporteXqm;
using TiendaVirtual.Dominio.Modelo.VendedorXqm;
using TiendaVirtual.Dominio.Modelo.VentaXqm;
using TiendaVirtual.Dominio.Utilidad;
using TiendaVirtual.Intercambio;

namespace TiendaVirtual.Dominio.Modelo.SeguridadXqm
{
    public class Usuario
    {
        public int UsuarioId { get; set; }

        [Required]
        public int PersonaId { get; set; }

        [Required]
        public string Correo { get; set; } = null!;

        [Required]
        public string Contrasena { get; set; } = null!;

        public bool CorreoConfirmado { get; set; }
        public bool ForzarCambioClave { get; set; }

        [EnumValorValido]
        public TipoEstadoUsuario Estado { get; set; }

        public DateTime FechaAlta { get; set; }
        public DateTime? UltimoAcceso { get; set; }

        // Relaciones
        public virtual Persona Persona { get; set; } = null!;
        public virtual ICollection<UsuarioRol> UsuarioRoles { get; set; } = new List<UsuarioRol>();
        public virtual ICollection<TokenRefresco> TokensRefresco { get; set; } = new List<TokenRefresco>();
        public virtual Vendedor? Vendedor { get; set; }
        public virtual ICollection<Carrito> Carritos { get; set; } = new List<Carrito>();
        public virtual ICollection<Orden> OrdenesComoCliente { get; set; } = new List<Orden>();
        public virtual ICollection<Transaccion> Transacciones { get; set; } = new List<Transaccion>();
        public virtual ICollection<Favorito> Favoritos { get; set; } = new List<Favorito>();
        public virtual ICollection<ResenaProducto> ResenasProducto { get; set; } = new List<ResenaProducto>();
        public virtual ICollection<ResenaVendedor> ResenasVendedor { get; set; } = new List<ResenaVendedor>();
        public virtual ICollection<Reclamo> ReclamosAbiertos { get; set; } = new List<Reclamo>();
        public virtual ICollection<Reclamo> ReclamosResueltos { get; set; } = new List<Reclamo>();
        public virtual ICollection<MensajeReclamo> MensajesReclamo { get; set; } = new List<MensajeReclamo>();
        public virtual ICollection<Notificacion> Notificaciones { get; set; } = new List<Notificacion>();
        public virtual ICollection<SolicitudVerificacion> SolicitudesVerificacionRevisadas { get; set; } = new List<SolicitudVerificacion>();
        public virtual ICollection<Retiro> RetirosProcesados { get; set; } = new List<Retiro>();

        public virtual ResultadoOperacion<bool> Validar()
        {
            return EntidadValidador.ValidarCamposRequeridos(this);
        }
    }
}
