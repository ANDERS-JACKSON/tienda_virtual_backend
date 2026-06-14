using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiendaVirtual.Intercambio.Dto.VendedorXqm
{
    public class EnviarSolicitudVerificacionDto
    {
        /// <summary>public_id de CDN (ej. tiendavirtual/abc) o URL absoluta legacy.</summary>
        [Required(ErrorMessage = "El documento (frente) es obligatorio.")]
        [MaxLength(500)]
        public string DocumentoFrenteUrl { get; set; } = null!;

        /// <summary>public_id de CDN o URL absoluta legacy.</summary>
        [MaxLength(500)]
        public string? DocumentoReversoUrl { get; set; }

        /// <summary>public_id de CDN o URL absoluta legacy.</summary>
        [MaxLength(500)]
        public string? SelfieDocumentoUrl { get; set; }

        // JSON con array de public_ids o URLs de fotos de productos
        public string? FotosProductos { get; set; }
    }
}
