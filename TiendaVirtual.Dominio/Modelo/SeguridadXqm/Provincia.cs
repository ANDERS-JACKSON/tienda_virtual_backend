namespace TiendaVirtual.Dominio.Modelo.SeguridadXqm
{
    public class Provincia
    {
        public string ProvinciaId { get; set; } = null!;
        public string DepartamentoId { get; set; } = null!;
        public string Nombre { get; set; } = null!;

        public virtual Departamento Departamento { get; set; } = null!;
        public virtual ICollection<Distrito> Distritos { get; set; } = new List<Distrito>();
    }
}
