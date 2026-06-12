using Microsoft.EntityFrameworkCore;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Modelo.PagoXqm;
using TiendaVirtual.Dominio.Utilidad;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.Sistema;
using TiendaVirtual.Intercambio.Dto.VentaXqm;

namespace TiendaVirtual.Dominio.Servicios.VentaXqm.Implementacion
{
    public partial class OrdenServicio
    {
        public async Task<ResultadoOperacion<PaginacionRespuestaDto<OrdenAdminListadoDto>>> ListarAdminAsync(
            string? busqueda, TipoEstadoOrden? estado, DateTime? fechaDesde, DateTime? fechaHasta,
            int pagina, int tamanioPagina)
        {
            try
            {
                pagina = Math.Max(1, pagina);
                tamanioPagina = Math.Clamp(tamanioPagina, 1, 50);

                var query = _context.Ordenes.AsNoTracking()
                    .Include(o => o.Cliente).ThenInclude(c => c.Persona)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(busqueda))
                {
                    var term = busqueda.Trim().ToLower();
                    query = query.Where(o =>
                        o.NumeroOrden.ToLower().Contains(term) ||
                        o.CorreoCliente.ToLower().Contains(term) ||
                        (o.TelefonoCliente != null && o.TelefonoCliente.Contains(term)));
                }
                if (estado.HasValue) query = query.Where(o => o.Estado == estado.Value);
                if (fechaDesde.HasValue) query = query.Where(o => o.Fecha >= fechaDesde.Value);
                if (fechaHasta.HasValue)
                    query = query.Where(o => o.Fecha < fechaHasta.Value.Date.AddDays(1));

                var total = await query.CountAsync();
                var ordenes = await query.OrderByDescending(o => o.OrdenId)
                    .Skip((pagina - 1) * tamanioPagina).Take(tamanioPagina).ToListAsync();

                var items = ordenes.Select(o => new OrdenAdminListadoDto
                {
                    OrdenId = o.OrdenId,
                    NumeroOrden = o.NumeroOrden,
                    CorreoCliente = o.CorreoCliente,
                    NombreCliente = o.Cliente?.Persona != null
                        ? $"{o.Cliente.Persona.Nombres} {o.Cliente.Persona.ApellidoPaterno}".Trim()
                        : o.CorreoCliente,
                    Total = o.Total,
                    Estado = new EnumeracionDto((int)o.Estado, o.Estado.GetDescription()),
                    CantidadSubordenes = o.Subordenes?.Count ?? 0,
                    FechaCreacion = o.Fecha
                }).ToList();

                // Fix suborden count - need separate query since not included
                foreach (var item in items)
                {
                    item.CantidadSubordenes = await _context.Subordenes
                        .CountAsync(s => s.OrdenId == item.OrdenId);
                }

                return ResultadoOperacion<PaginacionRespuestaDto<OrdenAdminListadoDto>>.SetExito(
                    new PaginacionRespuestaDto<OrdenAdminListadoDto>
                    {
                        Items = items, Pagina = pagina, TamanioPagina = tamanioPagina,
                        TotalRegistros = total, HayMas = pagina * tamanioPagina < total
                    });
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<PaginacionRespuestaDto<OrdenAdminListadoDto>>.SetError("Error: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<OrdenAdminResumenDto>> ObtenerResumenAdminAsync()
        {
            try
            {
                var hoy = DateTime.UtcNow.Date;
                var inicioMes = new DateTime(hoy.Year, hoy.Month, 1, 0, 0, 0, DateTimeKind.Utc);

                var totalHoy = await _context.Ordenes.CountAsync(o => o.Fecha >= hoy);
                var totalMes = await _context.Ordenes.CountAsync(o => o.Fecha >= inicioMes);
                var pendientes = await _context.Ordenes
                    .CountAsync(o => o.Estado == TipoEstadoOrden.PendientePago);

                return ResultadoOperacion<OrdenAdminResumenDto>.SetExito(new OrdenAdminResumenDto
                {
                    TotalOrdenesHoy = totalHoy,
                    TotalOrdenesMes = totalMes,
                    PendientesPago = pendientes
                });
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<OrdenAdminResumenDto>.SetError("Error: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<OrdenDto>> ObtenerAdminDetalleAsync(long ordenId)
        {
            var orden = await _context.Ordenes.AsNoTracking()
                .FirstOrDefaultAsync(o => o.OrdenId == ordenId);
            if (orden == null)
                return ResultadoOperacion<OrdenDto>.SetError("Orden no encontrada.");
            return await ObtenerMiOrdenAsync(orden.ClienteId, ordenId);
        }

        public async Task<ResultadoOperacion<bool>> CancelarAdminAsync(long ordenId, CancelarOrdenAdminDto dto)
        {
            using var trx = await _context.Database.BeginTransactionAsync();
            try
            {
                if (dto == null || string.IsNullOrWhiteSpace(dto.Motivo) || dto.Motivo.Trim().Length < 10)
                    return ResultadoOperacion<bool>.SetError("El motivo debe tener al menos 10 caracteres.");

                var orden = await _context.Ordenes
                    .Include(o => o.Subordenes)
                    .FirstOrDefaultAsync(o => o.OrdenId == ordenId);
                if (orden == null) return ResultadoOperacion<bool>.SetError("Orden no encontrada.");

                if (orden.Estado != TipoEstadoOrden.PendientePago && orden.Estado != TipoEstadoOrden.Pagada)
                    return ResultadoOperacion<bool>.SetError(
                        "Solo se pueden cancelar órdenes en PendientePago o Pagada.");

                var eraPagada = orden.Estado == TipoEstadoOrden.Pagada;
                orden.Estado = TipoEstadoOrden.Cancelada;
                foreach (var s in orden.Subordenes)
                    s.Estado = TipoEstadoSuborden.Cancelada;

                if (eraPagada)
                {
                    _context.Transacciones.Add(new Transaccion
                    {
                        OrdenId = orden.OrdenId,
                        UsuarioId = orden.ClienteId,
                        Proveedor = "MANUAL",
                        Tipo = TipoTransaccion.Reembolso,
                        Monto = orden.Total,
                        Estado = TipoEstadoTransaccion.Pendiente,
                        MetodoPago = "Reembolso admin",
                        Fecha = DateTime.UtcNow
                    });
                }

                await _context.SaveChangesAsync();
                await trx.CommitAsync();

                await _notificacionServicio.CrearAsync(
                    orden.ClienteId,
                    TipoNotificacion.OrdenCanceladaAdmin,
                    "Tu pedido fue cancelado",
                    $"El administrador canceló tu pedido {orden.NumeroOrden}. Motivo: {dto.Motivo.Trim()}");

                return ResultadoOperacion<bool>.SetExito(true);
            }
            catch (Exception ex)
            {
                await trx.RollbackAsync();
                return ResultadoOperacion<bool>.SetError("Error: " + ex.Message);
            }
        }
    }
}
