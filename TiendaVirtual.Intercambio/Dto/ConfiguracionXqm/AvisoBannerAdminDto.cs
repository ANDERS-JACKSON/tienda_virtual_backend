namespace TiendaVirtual.Intercambio.Dto.ConfiguracionXqm
{
    public class AvisoBannerAdminDto
    {
        public int AvisoBannerId { get; set; }
        public string Texto { get; set; } = null!;
        public bool Activo { get; set; }
        public int Orden { get; set; }
    }
}
