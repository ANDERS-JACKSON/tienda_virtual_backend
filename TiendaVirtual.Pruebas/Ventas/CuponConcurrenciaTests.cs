using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio;
using TiendaVirtual.Dominio.Modelo.VentaXqm;
using Xunit;

namespace TiendaVirtual.Pruebas.Ventas
{
    public class CuponConcurrenciaTests
    {
        [Fact]
        public async Task Cupon_DosCompradoresGastanElUltimoUso_ElSegundoEnGuardarFalla()
        {
            var nombreBd = Guid.NewGuid().ToString();
            var opciones = new DbContextOptionsBuilder<TiendaVirtualDbContext>()
                .UseInMemoryDatabase(nombreBd)
                .Options;

            using (var siembra = new TiendaVirtualDbContext(opciones))
            {
                siembra.CuponesPedido.Add(new CuponPedido
                {
                    CuponPedidoId = 1,
                    Codigo = "ULTIMO",
                    TipoDescuento = TipoDescuentoCupon.MontoFijo,
                    ValorDescuento = 10,
                    UsosMaximos = 1,
                    UsosRealizados = 0,
                    Activo = true,
                    FechaCreacion = DateTime.UtcNow,
                });
                await siembra.SaveChangesAsync();
            }

            using var compradorA = new TiendaVirtualDbContext(opciones);
            using var compradorB = new TiendaVirtualDbContext(opciones);

            var cuponA = await compradorA.CuponesPedido.FirstAsync(c => c.CuponPedidoId == 1);
            var cuponB = await compradorB.CuponesPedido.FirstAsync(c => c.CuponPedidoId == 1);

            cuponB.UsosRealizados += 1;
            compradorB.Entry(cuponB).Property<uint>("xmin").CurrentValue = 2;
            await compradorB.SaveChangesAsync();

            cuponA.UsosRealizados += 1;
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
                () => compradorA.SaveChangesAsync());

            using var verificador = new TiendaVirtualDbContext(opciones);
            var final = await verificador.CuponesPedido.FirstAsync(c => c.CuponPedidoId == 1);
            Assert.Equal(1, final.UsosRealizados);
        }
    }
}
