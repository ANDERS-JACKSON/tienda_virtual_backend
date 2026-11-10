namespace TiendaVirtual.Intercambio.Dto.ConfiguracionXqm
{
    public class FranjaInicioDto
    {
        public int FranjaInicioId { get; set; }
        public string Zona { get; set; } = null!;
        public string ImagenUrl { get; set; } = null!;
        public string? Titulo { get; set; }
        public string EnlaceUrl { get; set; } = null!;
        public int Orden { get; set; }
    }

    public class FranjaInicioAdminDto
    {
        public int FranjaInicioId { get; set; }
        public string Zona { get; set; } = null!;
        public string ImagenUrl { get; set; } = null!;
        public string? Titulo { get; set; }
        public string EnlaceUrl { get; set; } = null!;
        public bool Activo { get; set; }
        public int Orden { get; set; }
    }

    public class CrearFranjaInicioDto
    {
        public string Zona { get; set; } = null!;
        public string ImagenUrl { get; set; } = null!;
        public string? Titulo { get; set; }
        public string EnlaceUrl { get; set; } = null!;
        public bool Activo { get; set; } = true;
        public int? Orden { get; set; }
    }

    public class ActualizarFranjaInicioDto
    {
        public string ImagenUrl { get; set; } = null!;
        public string? Titulo { get; set; }
        public string EnlaceUrl { get; set; } = null!;
        public bool Activo { get; set; }
        public int Orden { get; set; }
    }
}
