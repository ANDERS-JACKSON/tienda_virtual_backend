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
        [Required]
        [Url]
        [MaxLength(500)]
        public string DocumentoFrenteUrl { get; set; } = null!;

        [Url]
        [MaxLength(500)]
        public string? DocumentoReversoUrl { get; set; }

        [Url]
        [MaxLength(500)]
        public string? SelfieDocumentoUrl { get; set; }

        // JSON con array de URLs de fotos de productos
        public string? FotosProductos { get; set; }
    }
}
