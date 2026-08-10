namespace TiendaVirtual.Dominio.Servicios.PagoXqm
{
    public interface IProveedorPagoFactory
    {
        IProveedorPagoServicio ObtenerActivo();
        IProveedorPagoServicio ObtenerPorCodigo(string codigoProveedor);
    }
}
