namespace TiendaVirtual.Intercambio.Dto.CatalogoXqm
{
    public class OpcionVarianteDto
    {
        public int OpcionVarianteId { get; set; }
        public string Nombre { get; set; } = null!;
        public int Orden { get; set; }
        public bool Activo { get; set; }
    }
}
