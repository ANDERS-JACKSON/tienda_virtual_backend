namespace TiendaVirtual.Dominio.Modelo.ConfiguracionXqm
{
    /// <summary>
    /// Tarjeta visual del inicio: foto + título + enlace.
    /// Zonas: campanas (encima de destacados) y categorias (debajo).
    /// Independiente del carrusel del hero (<see cref="CarruselInicio"/>).
    /// </summary>
    public class FranjaInicio
    {
        public const string ZonaCampanas = "campanas";
        public const string ZonaCategorias = "categorias";
        public const int MaxPorZona = 12;

        public int FranjaInicioId { get; set; }
        public string Zona { get; set; } = null!;
        public string ImagenUrl { get; set; } = null!;
        public string? Titulo { get; set; }
        public string EnlaceUrl { get; set; } = null!;
        public bool Activo { get; set; } = true;
        public int Orden { get; set; }

        public static bool EsZonaValida(string? zona) =>
            zona == ZonaCampanas || zona == ZonaCategorias;
    }
}
