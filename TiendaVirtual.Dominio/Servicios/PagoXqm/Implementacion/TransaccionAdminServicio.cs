using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Modelo.PagoXqm;
using TiendaVirtual.Dominio.Utilidad;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.PagoXqm;
using TiendaVirtual.Intercambio.Dto.Sistema;

namespace TiendaVirtual.Dominio.Servicios.PagoXqm.Implementacion
{
    public class TransaccionAdminServicio : ITransaccionAdminServicio
    {
        private readonly TiendaVirtualDbContext _context;
        private readonly ILogger<TransaccionAdminServicio> _logger;

        public TransaccionAdminServicio(TiendaVirtualDbContext context, ILogger<TransaccionAdminServicio> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ResultadoOperacion<PaginacionRespuestaDto<TransaccionAdminListadoDto>>> ListarAsync(
            TipoTransaccion? tipo, TipoEstadoTransaccion? estado,
            DateTime? fechaDesde, DateTime? fechaHasta, int pagina, int tamanioPagina)
        {
            try
            {
                pagina = Math.Max(1, pagina);
                tamanioPagina = Math.Clamp(tamanioPagina, 1, 50);

                var query = _context.Transacciones.AsNoTracking().AsQueryable();
                if (tipo.HasValue) query = query.Where(t => t.Tipo == tipo.Value);
                if (estado.HasValue) query = query.Where(t => t.Estado == estado.Value);
                if (fechaDesde.HasValue) query = query.Where(t => t.Fecha >= fechaDesde.Value);
                if (fechaHasta.HasValue)
                    query = query.Where(t => t.Fecha < fechaHasta.Value.Date.AddDays(1));

                var total = await query.CountAsync();
                var items = await query.OrderByDescending(t => t.TransaccionId)
                    .Skip((pagina - 1) * tamanioPagina).Take(tamanioPagina)
                    .Select(t => new TransaccionAdminListadoDto
                    {
                        TransaccionId = t.TransaccionId,
                        TransaccionProveedorId = t.TransaccionProveedorId,
                        Tipo = new EnumeracionDto((int)t.Tipo, t.Tipo.GetDescription()),
                        Monto = t.Monto,
                        Estado = new EnumeracionDto((int)t.Estado, t.Estado.GetDescription()),
                        MetodoPago = t.MetodoPago,
                        Fecha = t.Fecha
                    }).ToListAsync();

                return ResultadoOperacion<PaginacionRespuestaDto<TransaccionAdminListadoDto>>.SetExito(
                    new PaginacionRespuestaDto<TransaccionAdminListadoDto>
                    {
                        Items = items, Pagina = pagina, TamanioPagina = tamanioPagina,
                        TotalRegistros = total, HayMas = pagina * tamanioPagina < total
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en TransaccionAdminServicio.ListarAsync");
                return ResultadoOperacion<PaginacionRespuestaDto<TransaccionAdminListadoDto>>.SetError("Ocurrió un error inesperado. Intente nuevamente.");
            }

        }

        public async Task<ResultadoOperacion<TransaccionAdminResumenDto>> ObtenerResumenAsync()
        {
            try
            {
                var hoy = DateTime.UtcNow.Date;
                var inicioMes = new DateTime(hoy.Year, hoy.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                var tiposIngreso = new[] { TipoTransaccion.PagoOrden, TipoTransaccion.PagoSuscripcion };

                var ingresosHoy = await _context.Transacciones
                    .Where(t => t.Estado == TipoEstadoTransaccion.Completada &&
                        tiposIngreso.Contains(t.Tipo) && t.Fecha >= hoy)
                    .SumAsync(t => (decimal?)t.Monto) ?? 0;

                var ingresosMes = await _context.Transacciones
                    .Where(t => t.Estado == TipoEstadoTransaccion.Completada &&
                        tiposIngreso.Contains(t.Tipo) && t.Fecha >= inicioMes)
                    .SumAsync(t => (decimal?)t.Monto) ?? 0;

                var pendiente = await _context.Transacciones
                    .Where(t => t.Estado == TipoEstadoTransaccion.Pendiente)
                    .SumAsync(t => (decimal?)t.Monto) ?? 0;

                return ResultadoOperacion<TransaccionAdminResumenDto>.SetExito(new TransaccionAdminResumenDto
                {
                    IngresosHoy = ingresosHoy,
                    IngresosMes = ingresosMes,
                    TotalPendiente = pendiente
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en TransaccionAdminServicio.ObtenerResumenAsync");
                return ResultadoOperacion<TransaccionAdminResumenDto>.SetError("Ocurrió un error inesperado. Intente nuevamente.");
            }

        }

        public async Task<ResultadoOperacion<TransaccionAdminDetalleDto>> ObtenerDetalleAsync(Guid transaccionId)
        {
            try
            {
                var t = await _context.Transacciones.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.TransaccionId == transaccionId);
                if (t == null) return ResultadoOperacion<TransaccionAdminDetalleDto>.SetError("Transacción no encontrada.");

                return ResultadoOperacion<TransaccionAdminDetalleDto>.SetExito(new TransaccionAdminDetalleDto
                {
                    TransaccionId = t.TransaccionId,
                    TransaccionProveedorId = t.TransaccionProveedorId,
                    Tipo = new EnumeracionDto((int)t.Tipo, t.Tipo.GetDescription()),
                    Monto = t.Monto,
                    Estado = new EnumeracionDto((int)t.Estado, t.Estado.GetDescription()),
                    MetodoPago = t.MetodoPago,
                    Fecha = t.Fecha,
                    Proveedor = t.Proveedor,
                    OrdenId = t.OrdenId,
                    SuscripcionId = t.SuscripcionId,
                    UsuarioId = t.UsuarioId,
                    RespuestaProveedor = t.RespuestaProveedor
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en TransaccionAdminServicio.ObtenerDetalleAsync");
                return ResultadoOperacion<TransaccionAdminDetalleDto>.SetError("Ocurrió un error inesperado. Intente nuevamente.");
            }

        }

        public async Task<ResultadoOperacion<bool>> MarcarCompletadaAsync(Guid transaccionId)
        {
            try
            {
                var t = await _context.Transacciones.FindAsync(transaccionId);
                if (t == null) return ResultadoOperacion<bool>.SetError("Transacción no encontrada.");
                if (t.Tipo != TipoTransaccion.Reembolso)
                    return ResultadoOperacion<bool>.SetError("Solo se pueden completar manualmente transacciones de reembolso.");
                if (t.Estado != TipoEstadoTransaccion.Pendiente)
                    return ResultadoOperacion<bool>.SetError("La transacción no está pendiente.");

                t.Estado = TipoEstadoTransaccion.Completada;
                await _context.SaveChangesAsync();
                return ResultadoOperacion<bool>.SetExito(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en TransaccionAdminServicio.MarcarCompletadaAsync");
                return ResultadoOperacion<bool>.SetError("Ocurrió un error inesperado. Intente nuevamente.");
            }

        }

        public async Task<ResultadoOperacion<bool>> MarcarFallidaAsync(Guid transaccionId, MarcarTransaccionFallidaDto dto)
        {
            try
            {
                if (dto == null || string.IsNullOrWhiteSpace(dto.Motivo))
                    return ResultadoOperacion<bool>.SetError("El motivo es obligatorio.");

                var t = await _context.Transacciones.FindAsync(transaccionId);
                if (t == null) return ResultadoOperacion<bool>.SetError("Transacción no encontrada.");

                t.Estado = TipoEstadoTransaccion.Fallida;
                t.RespuestaProveedor = (t.RespuestaProveedor ?? "{}") + $" | Fallida admin: {dto.Motivo.Trim()}";
                await _context.SaveChangesAsync();
                return ResultadoOperacion<bool>.SetExito(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en TransaccionAdminServicio.MarcarFallidaAsync");
                return ResultadoOperacion<bool>.SetError("Ocurrió un error inesperado. Intente nuevamente.");
            }
        }
    }
}
