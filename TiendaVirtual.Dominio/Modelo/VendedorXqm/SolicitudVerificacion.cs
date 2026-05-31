using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Modelo.SeguridadXqm;
using TiendaVirtual.Dominio.Utilidad;
using TiendaVirtual.Intercambio;

namespace TiendaVirtual.Dominio.Modelo.VendedorXqm
{
    public class SolicitudVerificacion
    {
        public int SolicitudId { get; set; }

        [Required]
        public int VendedorId { get; set; }

        [EnumValorValido]
        public TipoEstadoSolicitudVerificacion Estado { get; set; }

        [Required]
        public string DocumentoFrenteUrl { get; set; } = null!;

        public string? DocumentoReversoUrl { get; set; }
        public string? SelfieDocumentoUrl { get; set; }
        public string? FotosProductos { get; set; }   // JSONB
        public int? VerificadorId { get; set; }
        public string? NotasRevisor { get; set; }
        public string? MotivoRechazo { get; set; }
        public DateTime FechaEnvio { get; set; }
        public DateTime? FechaRevision { get; set; }

        public virtual Vendedor Vendedor { get; set; } = null!;
        public virtual Usuario? Verificador { get; set; }

        public ResultadoOperacion<bool> Validar()
        {
            return EntidadValidador.ValidarCamposRequeridos(this);
        }
    }
}
