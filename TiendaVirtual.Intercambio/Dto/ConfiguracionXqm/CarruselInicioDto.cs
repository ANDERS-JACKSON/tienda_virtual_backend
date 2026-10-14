namespace TiendaVirtual.Intercambio.Dto.ConfiguracionXqm
{
    public class CarruselInicioDto
    {
        public int CarruselInicioId { get; set; }
        public string ImagenUrl { get; set; } = null!;
        public string? Titulo { get; set; }
        public string? Subtitulo { get; set; }
        public string? EnlaceUrl { get; set; }
        public string? TextoBoton { get; set; }
        public int Orden { get; set; }
    }

    public class CarruselInicioAdminDto
    {
        public int CarruselInicioId { get; set; }
        public string ImagenUrl { get; set; } = null!;
        public string? Titulo { get; set; }
        public string? Subtitulo { get; set; }
        public string? EnlaceUrl { get; set; }
        public string? TextoBoton { get; set; }
        public bool Activo { get; set; }
        public int Orden { get; set; }
    }

    public class CrearCarruselInicioDto
    {
        public string ImagenUrl { get; set; } = null!;
        public string? Titulo { get; set; }
        public string? Subtitulo { get; set; }
        public string? EnlaceUrl { get; set; }
        public string? TextoBoton { get; set; }
        public bool Activo { get; set; } = true;
        public int? Orden { get; set; }
    }

    public class ActualizarCarruselInicioDto
    {
        public string ImagenUrl { get; set; } = null!;
        public string? Titulo { get; set; }
        public string? Subtitulo { get; set; }
        public string? EnlaceUrl { get; set; }
        public string? TextoBoton { get; set; }
        public bool Activo { get; set; }
        public int Orden { get; set; }
    }
}
