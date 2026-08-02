namespace TiendaVirtual.Dominio.Modelo.SeguridadXqm
{
    public class Distrito
    {
        public string DistritoId { get; set; } = null!;
        public string ProvinciaId { get; set; } = null!;
        public string Nombre { get; set; } = null!;

        public virtual Provincia Provincia { get; set; } = null!;
    }
}
