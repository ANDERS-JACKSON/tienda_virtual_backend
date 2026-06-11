using System.ComponentModel.DataAnnotations;
using TiendaVirtual.Intercambio.Dto.Sistema;

namespace TiendaVirtual.Intercambio.Dto.SoporteXqm
{
    public class CrearResenaProductoDto
    {
        [Required]
        public long ItemOrdenId { get; set; }

        [Range(1, 5)]
        public int Calificacion { get; set; }

        [MaxLength(200)]
        public string? Titulo { get; set; }

        [MaxLength(1000)]
        public string? Comentario { get; set; }

        public List<string> Imagenes { get; set; } = new();
    }

    public class CrearResenaVendedorDto
    {
        [Required]
        public long SubordenId { get; set; }

        [Range(1, 5)]
        public int Calificacion { get; set; }

        [MaxLength(1000)]
        public string? Comentario { get; set; }
    }

    public class ResponderResenaDto
    {
        [Required, MaxLength(1000)]
        public string Respuesta { get; set; } = null!;
    }

    public class PendienteResenaDto
    {
        public long ItemOrdenId { get; set; }
        public int ProductoId { get; set; }
        public string NombreProducto { get; set; } = null!;
        public string? ImagenUrl { get; set; }
        public long SubordenId { get; set; }
        public int VendedorId { get; set; }
        public string NombreTienda { get; set; } = null!;
        public bool ResenoProducto { get; set; }
        public bool ResenoVendedor { get; set; }
    }
}
