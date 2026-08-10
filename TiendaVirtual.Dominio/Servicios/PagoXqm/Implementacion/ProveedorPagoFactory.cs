using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Opciones;

namespace TiendaVirtual.Dominio.Servicios.PagoXqm.Implementacion
{
    public sealed class ProveedorPagoFactory : IProveedorPagoFactory
    {
        private readonly IServiceProvider _sp;
        private readonly PagoProveedorOpciones _opciones;

        public ProveedorPagoFactory(IServiceProvider sp, IOptions<PagoProveedorOpciones> opciones)
        {
            _sp = sp;
            _opciones = opciones.Value;
        }

        public IProveedorPagoServicio ObtenerActivo()
        {
            var codigo = (_opciones.Activo ?? CodigoProveedorPago.MercadoPago).Trim().ToUpperInvariant();
            return ObtenerPorCodigo(codigo);
        }

        public IProveedorPagoServicio ObtenerPorCodigo(string codigoProveedor)
        {
            var codigo = (codigoProveedor ?? string.Empty).Trim().ToUpperInvariant();
            if (string.IsNullOrEmpty(codigo))
                throw new InvalidOperationException("Código de proveedor de pago vacío.");

            return _sp.GetRequiredKeyedService<IProveedorPagoServicio>(codigo);
        }
    }
}
