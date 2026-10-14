using System.ComponentModel.DataAnnotations;

namespace TiendaVirtual.Dominio.Modelo.ConfiguracionXqm
{
    public class CarruselInicio
    {
        public int CarruselInicioId { get; set; }

        [Required]
        public string ImagenUrl { get; set; } = null!;

        public string? Titulo { get; set; }
        public string? Subtitulo { get; set; }
        public string? EnlaceUrl { get; set; }
        public string? TextoBoton { get; set; }
        public bool Activo { get; set; }
        public int Orden { get; set; }
    }
}
