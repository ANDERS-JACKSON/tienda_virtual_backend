namespace TiendaVirtual.Dominio.Modelo.CatalogoXqm
{
    public class ProductoDestacado
    {
        public int ProductoDestacadoId { get; set; }
        public int ProductoId { get; set; }
        public int Orden { get; set; }

        public virtual Producto Producto { get; set; } = null!;
    }
}
