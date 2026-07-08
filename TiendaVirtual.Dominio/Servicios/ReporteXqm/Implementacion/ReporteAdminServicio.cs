using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Modelo.PagoXqm;
using TiendaVirtual.Dominio.Utilidad;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.ReporteXqm;
using TiendaVirtual.Intercambio.Dto.Sistema;

namespace TiendaVirtual.Dominio.Servicios.ReporteXqm.Implementacion
{
    public class ReporteAdminServicio : IReporteAdminServicio
    {
        private const string CacheDashboardPrefix = "reporte_admin_dashboard_";
        private static readonly TimeSpan CacheDashboardDuracion = TimeSpan.FromMinutes(5);

        private static readonly TipoTransaccion[] TiposIngreso =
        {
            TipoTransaccion.PagoOrden,
            TipoTransaccion.PagoSuscripcion
        };

        private static readonly TipoEstadoOrden[] EstadosOrdenPagada =
        {
            TipoEstadoOrden.Pagada,
            TipoEstadoOrden.EnPreparacion,
            TipoEstadoOrden.EnCamino,
            TipoEstadoOrden.Entregada,
            TipoEstadoOrden.Disputada
        };

        private readonly TiendaVirtualDbContext _context;
        private readonly ILogger<ReporteAdminServicio> _logger;
        private readonly IMemoryCache _cache;

        public ReporteAdminServicio(TiendaVirtualDbContext context, IMemoryCache cache, ILogger<ReporteAdminServicio> logger)
        {
            _logger = logger;
            _context = context;
            _cache = cache;
        }

        public async Task<ResultadoOperacion<ReporteAdminTotalesDto>> ObtenerTotalesDashboardAsync()
        {
            try
            {
                var hoy = DateTime.UtcNow.Date;
                var inicioMes = new DateTime(hoy.Year, hoy.Month, 1, 0, 0, 0, DateTimeKind.Utc);

                var verificacionesPendientes = await _context.SolicitudesVerificacion.AsNoTracking()
                    .CountAsync(s => s.Estado == TipoEstadoSolicitudVerificacion.Enviada);

                var vendedoresActivos = await _context.Vendedores.AsNoTracking()
                    .CountAsync(v => v.Estado == TipoEstadoVendedor.Activo);

                var productosPublicados = await _context.Productos.AsNoTracking()
                    .CountAsync(p => p.Estado == TipoEstadoProducto.Activo);

                var ordenesMes = await _context.Ordenes.AsNoTracking()
                    .CountAsync(o => o.Fecha >= inicioMes);

                var ingresosMes = await _context.Transacciones.AsNoTracking()
                    .Where(t => t.Estado == TipoEstadoTransaccion.Completada &&
                        TiposIngreso.Contains(t.Tipo) && t.Fecha >= inicioMes)
                    .SumAsync(t => (decimal?)t.Monto) ?? 0;

                return ResultadoOperacion<ReporteAdminTotalesDto>.SetExito(new ReporteAdminTotalesDto
                {
                    VerificacionesPendientes = verificacionesPendientes,
                    VendedoresActivos = vendedoresActivos,
                    ProductosPublicados = productosPublicados,
                    OrdenesMes = ordenesMes,
                    IngresosMes = ingresosMes
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en ReporteAdminServicio.ObtenerTotalesDashboardAsync");
                return ResultadoOperacion<ReporteAdminTotalesDto>.SetError("Ocurrió un error inesperado. Intente nuevamente.");
            }

        }

        public async Task<ResultadoOperacion<ReporteAdminDashboardDto>> ObtenerDashboardAsync(
            int? anio = null, int dias = 30)
        {
            try
            {
                var periodo = ResolverPeriodo(anio, dias);
                var cacheKey = $"{CacheDashboardPrefix}{periodo.Anio}_{periodo.DiasDiarios}";

                if (_cache.TryGetValue(cacheKey, out ReporteAdminDashboardDto? enCache) && enCache != null)
                    return ResultadoOperacion<ReporteAdminDashboardDto>.SetExito(enCache);

                var resumen = await ObtenerResumenAnioAsync(periodo);
                var ingresosMensuales = await ObtenerIngresosMensualesAsync(periodo);
                var ordenesDiarias = await ObtenerOrdenesDiariasAsync(periodo);
                var productosMasVendidos = await ObtenerProductosTopAsync(periodo);
                var clientesTop = await ObtenerClientesTopAsync(periodo);
                var vendedoresTop = await ObtenerVendedoresTopAsync(periodo);
                var ventasPorCategoria = await ObtenerVentasPorCategoriaAsync(periodo);
                var ordenesPorEstado = await ObtenerOrdenesPorEstadoAsync(periodo);

                var dashboard = new ReporteAdminDashboardDto
                {
                    Anio = periodo.Anio,
                    Resumen = resumen,
                    IngresosMensuales = ingresosMensuales,
                    OrdenesDiarias = ordenesDiarias,
                    ProductosMasVendidos = productosMasVendidos,
                    ClientesTop = clientesTop,
                    VendedoresTop = vendedoresTop,
                    VentasPorCategoria = ventasPorCategoria,
                    OrdenesPorEstado = ordenesPorEstado
                };

                _cache.Set(cacheKey, dashboard, CacheDashboardDuracion);
                return ResultadoOperacion<ReporteAdminDashboardDto>.SetExito(dashboard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en ReporteAdminServicio.ObtenerDashboardAsync");
                return ResultadoOperacion<ReporteAdminDashboardDto>.SetError("Ocurrió un error inesperado. Intente nuevamente.");
            }
        }

        private static PeriodoReporte ResolverPeriodo(int? anio, int dias)
        {
            var anioUtc = DateTime.UtcNow.Year;
            var anioEfectivo = Math.Clamp(anio ?? anioUtc, 2020, anioUtc);
            dias = Math.Clamp(dias, 7, 90);

            var inicioAnio = new DateTime(anioEfectivo, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var finAnio = inicioAnio.AddYears(1);
            var esAnioActual = anioEfectivo == anioUtc;
            var hoy = DateTime.UtcNow.Date;

            var finDiarioExclusivo = esAnioActual ? hoy.AddDays(1) : finAnio;
            var inicioDiario = finDiarioExclusivo.AddDays(-dias);
            if (inicioDiario < inicioAnio)
                inicioDiario = inicioAnio;

            var diasEfectivos = Math.Max(1, (int)(finDiarioExclusivo - inicioDiario).TotalDays);

            return new PeriodoReporte(
                anioEfectivo,
                inicioAnio,
                finAnio,
                inicioDiario,
                finDiarioExclusivo,
                diasEfectivos,
                esAnioActual,
                hoy);
        }

        private async Task<ReporteAdminResumenDto> ObtenerResumenAnioAsync(PeriodoReporte periodo)
        {
            var ingresosAnio = await _context.Transacciones.AsNoTracking()
                .Where(t => t.Estado == TipoEstadoTransaccion.Completada &&
                    TiposIngreso.Contains(t.Tipo) &&
                    t.Fecha >= periodo.InicioAnio && t.Fecha < periodo.FinAnio)
                .SumAsync(t => (decimal?)t.Monto) ?? 0;

            var ordenesAnio = await _context.Ordenes.AsNoTracking()
                .CountAsync(o => o.Fecha >= periodo.InicioAnio && o.Fecha < periodo.FinAnio);

            var ticketPromedio = await _context.Ordenes.AsNoTracking()
                .Where(o => o.Fecha >= periodo.InicioAnio && o.Fecha < periodo.FinAnio &&
                    EstadosOrdenPagada.Contains(o.Estado))
                .AverageAsync(o => (decimal?)o.Total) ?? 0;

            var comisionAnio = await _context.Subordenes.AsNoTracking()
                .Where(s => s.Estado == TipoEstadoSuborden.Entregada &&
                    s.Orden.Fecha >= periodo.InicioAnio && s.Orden.Fecha < periodo.FinAnio)
                .SumAsync(s => (decimal?)s.MontoComision) ?? 0;

            var clientesConCompras = await _context.Ordenes.AsNoTracking()
                .Where(o => o.Fecha >= periodo.InicioAnio && o.Fecha < periodo.FinAnio &&
                    EstadosOrdenPagada.Contains(o.Estado))
                .Select(o => o.ClienteId)
                .Distinct()
                .CountAsync();

            decimal ingresosHoy = 0;
            var ordenesHoy = 0;
            if (periodo.EsAnioActual)
            {
                ingresosHoy = await _context.Transacciones.AsNoTracking()
                    .Where(t => t.Estado == TipoEstadoTransaccion.Completada &&
                        TiposIngreso.Contains(t.Tipo) && t.Fecha >= periodo.Hoy)
                    .SumAsync(t => (decimal?)t.Monto) ?? 0;

                ordenesHoy = await _context.Ordenes.AsNoTracking()
                    .CountAsync(o => o.Fecha >= periodo.Hoy);
            }

            var vendedoresActivos = await _context.Vendedores.AsNoTracking()
                .CountAsync(v => v.Estado == TipoEstadoVendedor.Activo);

            var productosPublicados = await _context.Productos.AsNoTracking()
                .CountAsync(p => p.Estado == TipoEstadoProducto.Activo);

            var verificacionesPendientes = await _context.SolicitudesVerificacion.AsNoTracking()
                .CountAsync(s => s.Estado == TipoEstadoSolicitudVerificacion.Enviada);

            return new ReporteAdminResumenDto
            {
                IngresosMes = ingresosAnio,
                IngresosHoy = ingresosHoy,
                OrdenesMes = ordenesAnio,
                OrdenesHoy = ordenesHoy,
                VendedoresActivos = vendedoresActivos,
                ProductosPublicados = productosPublicados,
                ClientesConCompras = clientesConCompras,
                VerificacionesPendientes = verificacionesPendientes,
                TicketPromedioMes = Math.Round(ticketPromedio, 2),
                ComisionPlataformaMes = comisionAnio
            };
        }

        private async Task<List<ReporteSerieTemporalDto>> ObtenerIngresosMensualesAsync(PeriodoReporte periodo)
        {
            var datos = await _context.Transacciones.AsNoTracking()
                .Where(t => t.Estado == TipoEstadoTransaccion.Completada &&
                    TiposIngreso.Contains(t.Tipo) &&
                    t.Fecha >= periodo.InicioAnio && t.Fecha < periodo.FinAnio)
                .GroupBy(t => t.Fecha.Month)
                .Select(g => new
                {
                    Mes = g.Key,
                    Monto = g.Sum(t => t.Monto),
                    Cantidad = g.Count()
                })
                .ToListAsync();

            return GenerarSerieMensualAnio(periodo.Anio, datos
                .ToDictionary(d => d.Mes, d => (d.Monto, d.Cantidad)));
        }

        private async Task<List<ReporteSerieTemporalDto>> ObtenerOrdenesDiariasAsync(PeriodoReporte periodo)
        {
            var datos = await _context.Ordenes.AsNoTracking()
                .Where(o => o.Fecha >= periodo.InicioDiario && o.Fecha < periodo.FinDiarioExclusivo)
                .GroupBy(o => o.Fecha.Date)
                .Select(g => new
                {
                    Fecha = g.Key,
                    Monto = g.Sum(o => o.Total),
                    Cantidad = g.Count()
                })
                .ToListAsync();

            return GenerarSerieDiaria(
                periodo.InicioDiario,
                periodo.DiasDiarios,
                datos.ToDictionary(d => d.Fecha, d => (d.Monto, d.Cantidad)));
        }

        private async Task<List<ReporteProductoTopDto>> ObtenerProductosTopAsync(PeriodoReporte periodo)
        {
            return await _context.ItemsOrden.AsNoTracking()
                .Where(i => i.Suborden.Estado == TipoEstadoSuborden.Entregada &&
                    i.Suborden.Orden.Fecha >= periodo.InicioAnio &&
                    i.Suborden.Orden.Fecha < periodo.FinAnio)
                .GroupBy(i => new { i.NombreProducto, i.ImagenUrl })
                .Select(g => new ReporteProductoTopDto
                {
                    NombreProducto = g.Key.NombreProducto,
                    ImagenUrl = g.Key.ImagenUrl,
                    UnidadesVendidas = g.Sum(i => i.Cantidad),
                    Ingresos = g.Sum(i => i.TotalLinea)
                })
                .OrderByDescending(p => p.UnidadesVendidas)
                .ThenByDescending(p => p.Ingresos)
                .Take(10)
                .ToListAsync();
        }

        private async Task<List<ReporteClienteTopDto>> ObtenerClientesTopAsync(PeriodoReporte periodo)
        {
            var agregados = await _context.Ordenes.AsNoTracking()
                .Where(o => o.Fecha >= periodo.InicioAnio && o.Fecha < periodo.FinAnio &&
                    EstadosOrdenPagada.Contains(o.Estado))
                .GroupBy(o => o.ClienteId)
                .Select(g => new
                {
                    ClienteId = g.Key,
                    Correo = g.Max(o => o.CorreoCliente),
                    TotalOrdenes = g.Count(),
                    GastoTotal = g.Sum(o => o.Total)
                })
                .OrderByDescending(c => c.GastoTotal)
                .ThenByDescending(c => c.TotalOrdenes)
                .Take(10)
                .ToListAsync();

            if (agregados.Count == 0)
                return new List<ReporteClienteTopDto>();

            var clienteIds = agregados.Select(c => c.ClienteId).ToList();
            var nombres = await _context.Usuarios.AsNoTracking()
                .Include(u => u.Persona)
                .Where(u => clienteIds.Contains(u.UsuarioId))
                .Select(u => new
                {
                    u.UsuarioId,
                    Nombre = u.Persona != null
                        ? (u.Persona.Nombres + " " + u.Persona.ApellidoPaterno).Trim()
                        : u.Correo
                })
                .ToDictionaryAsync(u => u.UsuarioId, u => u.Nombre);

            return agregados.Select(c => new ReporteClienteTopDto
            {
                ClienteId = c.ClienteId,
                Correo = c.Correo,
                NombreCliente = nombres.GetValueOrDefault(c.ClienteId) ?? c.Correo,
                TotalOrdenes = c.TotalOrdenes,
                GastoTotal = c.GastoTotal
            }).ToList();
        }

        private async Task<List<ReporteVendedorTopDto>> ObtenerVendedoresTopAsync(PeriodoReporte periodo)
        {
            return await _context.Subordenes.AsNoTracking()
                .Where(s => s.Estado == TipoEstadoSuborden.Entregada &&
                    s.Orden.Fecha >= periodo.InicioAnio && s.Orden.Fecha < periodo.FinAnio)
                .GroupBy(s => new { s.VendedorId, s.Vendedor.NombreTienda })
                .Select(g => new ReporteVendedorTopDto
                {
                    VendedorId = g.Key.VendedorId,
                    NombreTienda = g.Key.NombreTienda,
                    VentasEntregadas = g.Count(),
                    IngresosGenerados = g.Sum(s => s.Subtotal + s.MontoEnvio),
                    ComisionPlataforma = g.Sum(s => s.MontoComision)
                })
                .OrderByDescending(v => v.IngresosGenerados)
                .ThenByDescending(v => v.VentasEntregadas)
                .Take(10)
                .ToListAsync();
        }

        private async Task<List<ReporteCategoriaDto>> ObtenerVentasPorCategoriaAsync(PeriodoReporte periodo)
        {
            return await _context.ItemsOrden.AsNoTracking()
                .Where(i => i.Suborden.Estado == TipoEstadoSuborden.Entregada &&
                    i.Suborden.Orden.Fecha >= periodo.InicioAnio &&
                    i.Suborden.Orden.Fecha < periodo.FinAnio &&
                    i.VarianteId != null &&
                    i.Variante!.Producto != null)
                .GroupBy(i => new
                {
                    i.Variante!.Producto!.CategoriaId,
                    i.Variante.Producto.Categoria!.Nombre
                })
                .Select(g => new ReporteCategoriaDto
                {
                    CategoriaId = g.Key.CategoriaId,
                    NombreCategoria = g.Key.Nombre,
                    UnidadesVendidas = g.Sum(i => i.Cantidad),
                    Ingresos = g.Sum(i => i.TotalLinea)
                })
                .OrderByDescending(c => c.Ingresos)
                .ThenByDescending(c => c.UnidadesVendidas)
                .Take(8)
                .ToListAsync();
        }

        private async Task<List<ReporteEstadoDto>> ObtenerOrdenesPorEstadoAsync(PeriodoReporte periodo)
        {
            var datos = await _context.Ordenes.AsNoTracking()
                .Where(o => o.Fecha >= periodo.InicioAnio && o.Fecha < periodo.FinAnio)
                .GroupBy(o => o.Estado)
                .Select(g => new
                {
                    Estado = g.Key,
                    Cantidad = g.Count(),
                    MontoTotal = g.Sum(o => o.Total)
                })
                .OrderByDescending(d => d.Cantidad)
                .ToListAsync();

            return datos.Select(d => new ReporteEstadoDto
            {
                Estado = new EnumeracionDto((int)d.Estado, d.Estado.GetDescription()),
                Cantidad = d.Cantidad,
                MontoTotal = d.MontoTotal
            }).ToList();
        }

        private static List<ReporteSerieTemporalDto> GenerarSerieMensualAnio(
            int anio,
            Dictionary<int, (decimal Monto, int Cantidad)> datos)
        {
            var serie = new List<ReporteSerieTemporalDto>();
            var cultura = new System.Globalization.CultureInfo("es-PE");

            for (var mes = 1; mes <= 12; mes++)
            {
                datos.TryGetValue(mes, out var valor);
                var fecha = new DateTime(anio, mes, 1, 0, 0, 0, DateTimeKind.Utc);
                serie.Add(new ReporteSerieTemporalDto
                {
                    Fecha = fecha,
                    Etiqueta = fecha.ToString("MMM", cultura),
                    Monto = valor.Monto,
                    Cantidad = valor.Cantidad
                });
            }

            return serie;
        }

        private static List<ReporteSerieTemporalDto> GenerarSerieDiaria(
            DateTime inicio, int dias,
            Dictionary<DateTime, (decimal Monto, int Cantidad)> datos)
        {
            var serie = new List<ReporteSerieTemporalDto>();
            var cursor = inicio.Date;
            var cultura = new System.Globalization.CultureInfo("es-PE");

            for (var i = 0; i < dias; i++)
            {
                datos.TryGetValue(cursor, out var valor);
                serie.Add(new ReporteSerieTemporalDto
                {
                    Fecha = cursor,
                    Etiqueta = cursor.ToString("dd MMM", cultura),
                    Monto = valor.Monto,
                    Cantidad = valor.Cantidad
                });
                cursor = cursor.AddDays(1);
            }

            return serie;
        }

        private sealed record PeriodoReporte(
            int Anio,
            DateTime InicioAnio,
            DateTime FinAnio,
            DateTime InicioDiario,
            DateTime FinDiarioExclusivo,
            int DiasDiarios,
            bool EsAnioActual,
            DateTime Hoy);
    }
}
