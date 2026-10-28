using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio;
using TiendaVirtual.Dominio.Modelo.CatalogoXqm;
using TiendaVirtual.Dominio.Modelo.VendedorXqm;
using TiendaVirtual.Dominio.Modelo.VentaXqm;
using TiendaVirtual.Dominio.Servicios.VentaXqm.Implementacion;
using TiendaVirtual.Pruebas.Fakes;
using Xunit;

namespace TiendaVirtual.Pruebas.Ventas
{
    /// <summary>
    /// C1: una orden cancelada (p. ej. por un pago fallido) o en disputa NO debe dar
    /// acceso al PDF del patrón, aunque esos estados tengan un valor numérico mayor
    /// que "Pagada" en el enum TipoEstadoOrden.
    /// </summary>
    public class OrdenServicioTests
    {
        private static (OrdenServicio Servicio, TiendaVirtualDbContext Contexto, Guid ClienteId, Guid OrdenId)
            CrearOrdenDePrueba(TipoEstadoOrden estadoOrden)
        {
            var opciones = new DbContextOptionsBuilder<TiendaVirtualDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var contexto = new TiendaVirtualDbContext(opciones);

            var clienteId = Guid.NewGuid();
            var vendedor = new Vendedor
            {
                VendedorId = 1,
                UsuarioId = Guid.NewGuid(),
                NombreTienda = "Tienda de prueba",
                SlugTienda = "tienda-de-prueba",
                Estado = TipoEstadoVendedor.Activo,
                VendePatrones = true,
            };
            contexto.Vendedores.Add(vendedor);

            var ordenId = Guid.NewGuid();
            var suborden = new Suborden
            {
                SubordenId = Guid.NewGuid(),
                OrdenId = ordenId,
                VendedorId = vendedor.VendedorId,
                NumeroSuborden = "SO-0001",
                Estado = TipoEstadoSuborden.Pendiente,
            };
            suborden.Items.Add(new ItemOrden
            {
                ItemOrdenId = Guid.NewGuid(),
                SubordenId = suborden.SubordenId,
                NombreProducto = "Patrón de tejido",
                PrecioUnitario = 25,
                Cantidad = 1,
                TotalLinea = 25,
                TipoProducto = TipoProducto.Patron,
                ArchivoPatronUrl = "https://cdn.example.com/patrones/secreto.pdf",
            });

            var orden = new Orden
            {
                OrdenId = ordenId,
                NumeroOrden = "OR-0001",
                ClienteId = clienteId,
                CorreoCliente = "cliente@example.com",
                DireccionEnvio = "{}",
                Estado = estadoOrden,
                Fecha = DateTime.UtcNow,
                Subtotal = 25,
                Total = 25,
            };
            orden.Subordenes.Add(suborden);
            contexto.Ordenes.Add(orden);
            contexto.SaveChanges();

            var servicio = new OrdenServicio(
                contexto, new NotificacionServicioFalso(), NullLogger<OrdenServicio>.Instance);

            return (servicio, contexto, clienteId, ordenId);
        }

        [Theory]
        [InlineData(TipoEstadoOrden.Cancelada)]
        [InlineData(TipoEstadoOrden.Disputada)]
        [InlineData(TipoEstadoOrden.PendientePago)]
        public async Task ObtenerMiOrden_NoExponeElPdfDelPatronSiLaOrdenNoEstaPagada(TipoEstadoOrden estado)
        {
            var (servicio, _, clienteId, ordenId) = CrearOrdenDePrueba(estado);

            var resultado = await servicio.ObtenerMiOrdenAsync(clienteId, ordenId);

            Assert.True(resultado.Exito);
            var item = resultado.Datos!.Subordenes.Single().Items.Single();
            Assert.Null(item.ArchivoPatronUrl);
        }

        [Theory]
        [InlineData(TipoEstadoOrden.Pagada)]
        [InlineData(TipoEstadoOrden.EnPreparacion)]
        [InlineData(TipoEstadoOrden.EnCamino)]
        [InlineData(TipoEstadoOrden.Entregada)]
        public async Task ObtenerMiOrden_ExponeElPdfDelPatronCuandoLaOrdenEstaPagadaOPosterior(TipoEstadoOrden estado)
        {
            var (servicio, _, clienteId, ordenId) = CrearOrdenDePrueba(estado);

            var resultado = await servicio.ObtenerMiOrdenAsync(clienteId, ordenId);

            Assert.True(resultado.Exito);
            var item = resultado.Datos!.Subordenes.Single().Items.Single();
            Assert.Equal("https://cdn.example.com/patrones/secreto.pdf", item.ArchivoPatronUrl);
        }

        [Fact]
        public async Task ExpirarReservas_CancelaPedidosPendientePagoViejosYLiberaStock()
        {
            var opciones = new DbContextOptionsBuilder<TiendaVirtualDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            var contexto = new TiendaVirtualDbContext(opciones);

            var clienteId = Guid.NewGuid();
            var vendedor = new Vendedor
            {
                VendedorId = 1,
                UsuarioId = Guid.NewGuid(),
                NombreTienda = "Tienda",
                SlugTienda = "tienda",
                Estado = TipoEstadoVendedor.Activo,
            };
            contexto.Vendedores.Add(vendedor);

            var producto = new Producto
            {
                ProductoId = 1,
                VendedorId = 1,
                CategoriaId = 1,
                Nombre = "Manta",
                Slug = "manta",
                Estado = TipoEstadoProducto.Activo,
                Tipo = TipoProducto.Fisico,
            };
            var variante = new VarianteProducto
            {
                VarianteId = 1,
                ProductoId = 1,
                Precio = 50,
                Activa = true,
            };
            variante.Stock = new Stock
            {
                VarianteId = 1,
                CantidadDisponible = 0,
                CantidadReservada = 2,
                UmbralStockBajo = 0,
            };
            contexto.Productos.Add(producto);
            contexto.VariantesProducto.Add(variante);

            var ordenId = Guid.NewGuid();
            var suborden = new Suborden
            {
                SubordenId = Guid.NewGuid(),
                OrdenId = ordenId,
                VendedorId = 1,
                NumeroSuborden = "SO-EXP",
                Estado = TipoEstadoSuborden.Pendiente,
            };
            suborden.Items.Add(new ItemOrden
            {
                ItemOrdenId = Guid.NewGuid(),
                SubordenId = suborden.SubordenId,
                VarianteId = 1,
                NombreProducto = "Manta",
                PrecioUnitario = 50,
                Cantidad = 2,
                TotalLinea = 100,
                TipoProducto = TipoProducto.Fisico,
            });
            contexto.Ordenes.Add(new Orden
            {
                OrdenId = ordenId,
                NumeroOrden = "OR-EXP",
                ClienteId = clienteId,
                CorreoCliente = "cliente@example.com",
                DireccionEnvio = "{}",
                Estado = TipoEstadoOrden.PendientePago,
                Fecha = DateTime.UtcNow.AddHours(-25),
                Subtotal = 100,
                Total = 100,
                Subordenes = { suborden },
            });
            await contexto.SaveChangesAsync();

            var servicio = new OrdenServicio(
                contexto, new NotificacionServicioFalso(), NullLogger<OrdenServicio>.Instance);

            var expiradas = await servicio.ExpirarReservasPendientePagoAsync(TimeSpan.FromHours(24));

            Assert.Equal(1, expiradas);
            var orden = await contexto.Ordenes.FirstAsync(o => o.OrdenId == ordenId);
            Assert.Equal(TipoEstadoOrden.Cancelada, orden.Estado);
            var stock = await contexto.Stocks.FirstAsync(s => s.VarianteId == 1);
            Assert.Equal(2, stock.CantidadDisponible);
            Assert.Equal(0, stock.CantidadReservada);
        }
    }
}
