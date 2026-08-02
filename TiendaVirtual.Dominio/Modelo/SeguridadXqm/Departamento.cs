namespace TiendaVirtual.Dominio.Modelo.SeguridadXqm
{
    public class Departamento
    {
        public string DepartamentoId { get; set; } = null!;
        public string Nombre { get; set; } = null!;

        public virtual ICollection<Provincia> Provincias { get; set; } = new List<Provincia>();
    }
}
