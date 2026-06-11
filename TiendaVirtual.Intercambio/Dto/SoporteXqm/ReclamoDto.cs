using System.ComponentModel.DataAnnotations;
using TiendaVirtual.Intercambio.Dto.Sistema;

namespace TiendaVirtual.Intercambio.Dto.SoporteXqm
{
    public class ReclamoDto
    {
        public long ReclamoId { get; set; }
        public long SubordenId { get; set; }
        public string NumeroSuborden { get; set; } = null!;
        public int VendedorId { get; set; }
        public string NombreTienda { get; set; } = null!;
        public int AbiertoPor { get; set; }
        public string NombreCliente { get; set; } = null!;
        public EnumeracionDto Motivo { get; set; } = null!;
        public string? Descripcion { get; set; }
        public List<string> Evidencias { get; set; } = new();
        public EnumeracionDto Estado { get; set; } = null!;
        public string? NotasResolucion { get; set; }
        public decimal? MontoReembolso { get; set; }
        public DateTime FechaApertura { get; set; }
        public DateTime? FechaResolucion { get; set; }
        public List<MensajeReclamoDto> Mensajes { get; set; } = new();
    }
}
