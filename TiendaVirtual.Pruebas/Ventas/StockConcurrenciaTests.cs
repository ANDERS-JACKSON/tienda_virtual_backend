using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TiendaVirtual.Dominio;
using TiendaVirtual.Dominio.Modelo.CatalogoXqm;
using Xunit;

namespace TiendaVirtual.Pruebas.Ventas
{
    /// <summary>
    /// A6: dos compras concurrentes no deben poder "pisarse" al descontar el
    /// mismo stock (lost update / sobreventa). `StockDB` mapea la columna de
    /// sistema `xmin` de Postgres como token de concurrencia optimista; este
    /// test reproduce el escenario de carrera usando dos DbContext distintos
    /// sobre la misma base InMemory, exactamente como lo harían dos requests
    /// HTTP concurrentes sobre la misma fila de stock.
    /// </summary>
    public class StockConcurrenciaTests
    {
        [Fact]
        public async Task Stock_DosCompradoresLeenElMismoStock_ElSegundoEnGuardarFalla()
        {
            var nombreBd = Guid.NewGuid().ToString();
            var opciones = new DbContextOptionsBuilder<TiendaVirtualDbContext>()
                .UseInMemoryDatabase(nombreBd)
                .Options;

            const int varianteId = 1;

            using (var siembra = new TiendaVirtualDbContext(opciones))
            {
                siembra.Stocks.Add(new Stock
                {
                    VarianteId = varianteId,
                    CantidadDisponible = 1, // última unidad
                    CantidadReservada = 0,
                    UmbralStockBajo = 0,
                });
                await siembra.SaveChangesAsync();
            }

            // Dos "compradores" leen el mismo stock casi al mismo tiempo, cada
            // uno con su propio DbContext (como dos requests HTTP distintos).
            using var compradorA = new TiendaVirtualDbContext(opciones);
            using var compradorB = new TiendaVirtualDbContext(opciones);

            var stockVistoPorA = await compradorA.Stocks.FirstAsync(s => s.VarianteId == varianteId);
            var stockVistoPorB = await compradorB.Stocks.FirstAsync(s => s.VarianteId == varianteId);

            // B confirma primero su compra y se lleva la última unidad. Simulamos
            // el avance de versión de fila que Postgres haría solo de verdad con
            // `xmin` (el proveedor InMemory no lo genera automáticamente).
            stockVistoPorB.CantidadDisponible -= 1;
            compradorB.Entry(stockVistoPorB).Property<uint>("xmin").CurrentValue = 2;
            await compradorB.SaveChangesAsync();

            // A leyó el stock ANTES de que B confirmara (misma versión de fila
            // original) e intenta reservar la misma unidad: debe rechazarse en
            // vez de sobrescribir en silencio el cambio de B.
            stockVistoPorA.CantidadDisponible -= 1;
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
                () => compradorA.SaveChangesAsync());

            // El stock final refleja solo la compra de B: nunca quedó en -1.
            using var verificador = new TiendaVirtualDbContext(opciones);
            var stockFinal = await verificador.Stocks.FirstAsync(s => s.VarianteId == varianteId);
            Assert.Equal(0, stockFinal.CantidadDisponible);
        }
    }
}
