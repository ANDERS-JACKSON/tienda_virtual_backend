using System.ComponentModel.DataAnnotations;
using TiendaVirtual.Intercambio.Dto.Sistema;

namespace TiendaVirtual.Intercambio.Dto.SoporteXqm
{
    public class ReclamoListadoDto
    {
        public long ReclamoId { get; set; }
        public long SubordenId { get; set; }
        public string NumeroSuborden { get; set; } = null!;
        public string NombreContraparte { get; set; } = null!;
        public EnumeracionDto Motivo { get; set; } = null!;
        public EnumeracionDto Estado { get; set; } = null!;
        public DateTime FechaApertura { get; set; }
        public int TotalMensajes { get; set; }
    }

    public class AbrirReclamoDto
    {
        [Required]
        public long SubordenId { get; set; }

        [Required]
        public EnumeracionDto Motivo { get; set; } = null!;

        [Required, MaxLength(1000)]
        public string Descripcion { get; set; } = null!;

        public List<string> Evidencias { get; set; } = new();
    }

    public class AgregarMensajeReclamoDto
    {
        [Required, MaxLength(2000)]
        public string Mensaje { get; set; } = null!;

        public List<string> Adjuntos { get; set; } = new();
    }

    public class ResolverReclamoDto
    {
        [Required]
        public EnumeracionDto Estado { get; set; } = null!;

        [MaxLength(1000)]
        public string? NotasResolucion { get; set; }

        public decimal? MontoReembolso { get; set; }
    }
}
