using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Modelo.SeguridadXqm;
using TiendaVirtual.Dominio.Modelo.SoporteXqm;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.Sistema;
using TiendaVirtual.Intercambio.Dto.SoporteXqm;

namespace TiendaVirtual.Dominio.Servicios.SoporteXqm.Implementacion
{
    public class ReclamoServicio : IReclamoServicio
    {
        private readonly TiendaVirtualDbContext _context;
        private readonly INotificacionServicio _notif;

        public ReclamoServicio(TiendaVirtualDbContext context, INotificacionServicio notif)
        {
            _context = context;
            _notif = notif;
        }

        public async Task<ResultadoOperacion<ReclamoDto>> AbrirAsync(int usuarioId, AbrirReclamoDto dto)
        {
            using var trx = await _context.Database.BeginTransactionAsync();
            try
            {
                var suborden = await _context.Subordenes
                    .Include(s => s.Orden)
                    .Include(s => s.Vendedor)
                    .FirstOrDefaultAsync(s => s.SubordenId == dto.SubordenId);

                if (suborden == null)
                    return ResultadoOperacion<ReclamoDto>.SetError("Suborden no encontrada.");

                if (suborden.Orden.ClienteId != usuarioId)
                    return ResultadoOperacion<ReclamoDto>.SetError(
                        "No puedes abrir un reclamo sobre una suborden que no es tuya.");

                var estadosReclamables = new[]
                {
                    TipoEstadoSuborden.EnCamino,
                    TipoEstadoSuborden.Entregada,
                    TipoEstadoSuborden.EnDisputa
                };
                if (!estadosReclamables.Contains(suborden.Estado))
                    return ResultadoOperacion<ReclamoDto>.SetError(
                        "Solo puedes reclamar pedidos que estén en camino, entregados o ya en disputa.");

                var yaTiene = await _context.Reclamos.AnyAsync(r =>
                    r.SubordenId == dto.SubordenId &&
                    (r.Estado == TipoEstadoReclamo.Abierto || r.Estado == TipoEstadoReclamo.EnRevision));
                if (yaTiene)
                    return ResultadoOperacion<ReclamoDto>.SetError("Ya tienes un reclamo abierto sobre este pedido.");

                var reclamo = new Reclamo
                {
                    SubordenId = dto.SubordenId,
                    AbiertoPor = usuarioId,
                    Motivo = (TipoMotivoReclamo)dto.Motivo.Id,
                    Descripcion = dto.Descripcion.Trim(),
                    Evidencias = dto.Evidencias.Count > 0 ? JsonSerializer.Serialize(dto.Evidencias) : null,
                    Estado = TipoEstadoReclamo.Abierto,
                    FechaApertura = DateTime.UtcNow
                };
                _context.Reclamos.Add(reclamo);
                suborden.Estado = TipoEstadoSuborden.EnDisputa;

                await _context.SaveChangesAsync();
                await trx.CommitAsync();

                await _notif.CrearAsync(
                    suborden.Vendedor.UsuarioId,
                    TipoNotificacion.ReclamoAbierto,
                    $"Reclamo abierto en pedido {suborden.NumeroSuborden}",
                    $"El cliente abrió un reclamo: \"{dto.Descripcion}\". Por favor revisa y responde.",
                    new { reclamoId = reclamo.ReclamoId, subordenId = suborden.SubordenId });

                return await ObtenerDetalleAsync(usuarioId, reclamo.ReclamoId);
            }
            catch (Exception ex)
            {
                await trx.RollbackAsync();
                return ResultadoOperacion<ReclamoDto>.SetError("Error: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<ReclamoDto>> ObtenerDetalleAsync(int usuarioId, long reclamoId)
        {
            try
            {
                var reclamo = await _context.Reclamos.AsNoTracking()
                    .Include(r => r.Suborden).ThenInclude(s => s.Vendedor)
                    .Include(r => r.AbiertoPorUsuario).ThenInclude(u => u.Persona)
                    .Include(r => r.Mensajes).ThenInclude(m => m.Remitente).ThenInclude(u => u.Persona)
                    .Include(r => r.Mensajes).ThenInclude(m => m.Remitente).ThenInclude(u => u.UsuarioRoles).ThenInclude(ur => ur.Rol)
                    .FirstOrDefaultAsync(r => r.ReclamoId == reclamoId);

                if (reclamo == null)
                    return ResultadoOperacion<ReclamoDto>.SetError("Reclamo no encontrado.");

                if (!await TieneAccesoAsync(usuarioId, reclamo))
                    return ResultadoOperacion<ReclamoDto>.SetError("No tienes acceso a este reclamo.");

                return ResultadoOperacion<ReclamoDto>.SetExito(MapearADto(reclamo));
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<ReclamoDto>.SetError("Error: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<MensajeReclamoDto>> AgregarMensajeAsync(
            int usuarioId, long reclamoId, AgregarMensajeReclamoDto dto)
        {
            try
            {
                var reclamo = await _context.Reclamos
                    .Include(r => r.Suborden).ThenInclude(s => s.Vendedor)
                    .FirstOrDefaultAsync(r => r.ReclamoId == reclamoId);

                if (reclamo == null)
                    return ResultadoOperacion<MensajeReclamoDto>.SetError("Reclamo no encontrado.");

                if (reclamo.Estado == TipoEstadoReclamo.Cerrado)
                    return ResultadoOperacion<MensajeReclamoDto>.SetError("Este reclamo ya está cerrado.");

                var esAdmin = await EsAdminOVerificadorAsync(usuarioId);
                var esCliente = reclamo.AbiertoPor == usuarioId;
                var esVendedor = reclamo.Suborden.Vendedor.UsuarioId == usuarioId;
                if (!esAdmin && !esCliente && !esVendedor)
                    return ResultadoOperacion<MensajeReclamoDto>.SetError("No puedes participar en este reclamo.");

                var mensaje = new MensajeReclamo
                {
                    ReclamoId = reclamoId,
                    RemitenteId = usuarioId,
                    Mensaje = dto.Mensaje.Trim(),
                    Adjuntos = dto.Adjuntos.Count > 0 ? JsonSerializer.Serialize(dto.Adjuntos) : null,
                    Fecha = DateTime.UtcNow
                };
                _context.MensajesReclamo.Add(mensaje);

                if (reclamo.Estado == TipoEstadoReclamo.Abierto && !esCliente)
                    reclamo.Estado = TipoEstadoReclamo.EnRevision;

                await _context.SaveChangesAsync();

                var destinatarioId = esCliente
                    ? reclamo.Suborden.Vendedor.UsuarioId
                    : reclamo.AbiertoPor;

                await _notif.CrearAsync(
                    destinatarioId,
                    TipoNotificacion.ReclamoMensajeNuevo,
                    $"Nuevo mensaje en reclamo del pedido {reclamo.Suborden.NumeroSuborden}",
                    dto.Mensaje.Length > 200 ? dto.Mensaje[..200] + "..." : dto.Mensaje,
                    new { reclamoId });

                var remitente = await _context.Usuarios
                    .Include(u => u.Persona)
                    .Include(u => u.UsuarioRoles).ThenInclude(ur => ur.Rol)
                    .FirstAsync(u => u.UsuarioId == usuarioId);

                return ResultadoOperacion<MensajeReclamoDto>.SetExito(new MensajeReclamoDto
                {
                    MensajeId = mensaje.MensajeId,
                    RemitenteId = usuarioId,
                    NombreRemitente = $"{remitente.Persona.Nombres} {remitente.Persona.ApellidoPaterno}".Trim(),
                    RolRemitente = ObtenerRolPrincipal(remitente),
                    Mensaje = mensaje.Mensaje,
                    Adjuntos = dto.Adjuntos,
                    Fecha = mensaje.Fecha
                });
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<MensajeReclamoDto>.SetError("Error: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<bool>> ResolverAsync(
            int usuarioId, long reclamoId, ResolverReclamoDto dto)
        {
            using var trx = await _context.Database.BeginTransactionAsync();
            try
            {
                if (!await EsAdminOVerificadorAsync(usuarioId))
                    return ResultadoOperacion<bool>.SetError("Solo admin/verificador puede resolver.");

                var reclamo = await _context.Reclamos
                    .Include(r => r.Suborden).ThenInclude(s => s.Vendedor)
                    .FirstOrDefaultAsync(r => r.ReclamoId == reclamoId);

                if (reclamo == null)
                    return ResultadoOperacion<bool>.SetError("Reclamo no encontrado.");

                var nuevoEstado = (TipoEstadoReclamo)dto.Estado.Id;
                var estadosValidos = new[]
                {
                    TipoEstadoReclamo.ResueltoCliente,
                    TipoEstadoReclamo.ResueltoVendedor,
                    TipoEstadoReclamo.Cerrado
                };
                if (!estadosValidos.Contains(nuevoEstado))
                    return ResultadoOperacion<bool>.SetError("Estado de resolución inválido.");

                reclamo.Estado = nuevoEstado;
                reclamo.NotasResolucion = dto.NotasResolucion?.Trim();
                reclamo.MontoReembolso = dto.MontoReembolso;
                reclamo.ResueltoPor = usuarioId;
                reclamo.FechaResolucion = DateTime.UtcNow;

                if (nuevoEstado == TipoEstadoReclamo.ResueltoCliente)
                    reclamo.Suborden.Estado = TipoEstadoSuborden.Cancelada;
                else if (nuevoEstado == TipoEstadoReclamo.ResueltoVendedor)
                    reclamo.Suborden.Estado = TipoEstadoSuborden.Entregada;

                await _context.SaveChangesAsync();
                await trx.CommitAsync();

                var titulo = $"Reclamo resuelto - Pedido {reclamo.Suborden.NumeroSuborden}";
                var cuerpo = $"El administrador resolvió tu reclamo: {nuevoEstado.GetDescription()}. " +
                             (dto.NotasResolucion ?? string.Empty);

                await _notif.CrearAsync(reclamo.AbiertoPor, TipoNotificacion.ReclamoResuelto, titulo, cuerpo,
                    new { reclamoId });
                await _notif.CrearAsync(reclamo.Suborden.Vendedor.UsuarioId, TipoNotificacion.ReclamoResuelto,
                    titulo, cuerpo, new { reclamoId });

                return ResultadoOperacion<bool>.SetExito(true);
            }
            catch (Exception ex)
            {
                await trx.RollbackAsync();
                return ResultadoOperacion<bool>.SetError("Error: " + ex.Message);
            }
        }

        public Task<ResultadoOperacion<PaginacionRespuestaDto<ReclamoListadoDto>>> ListarMisAsync(
            int usuarioId, int pagina, int tamanioPagina)
        {
            pagina = Math.Max(1, pagina);
            tamanioPagina = Math.Clamp(tamanioPagina, 1, 50);

            var query = _context.Reclamos.AsNoTracking()
                .Where(r => r.AbiertoPor == usuarioId);

            return ListarPaginadoAsync(query, pagina, tamanioPagina, esCliente: true);
        }

        public Task<ResultadoOperacion<PaginacionRespuestaDto<ReclamoListadoDto>>> ListarRecibidosAsync(
            int usuarioId, int pagina, int tamanioPagina)
        {
            pagina = Math.Max(1, pagina);
            tamanioPagina = Math.Clamp(tamanioPagina, 1, 50);

            var query = _context.Reclamos.AsNoTracking()
                .Where(r => r.Suborden.Vendedor.UsuarioId == usuarioId);

            return ListarPaginadoAsync(query, pagina, tamanioPagina, esCliente: false);
        }

        public Task<ResultadoOperacion<PaginacionRespuestaDto<ReclamoListadoDto>>> ListarAdminAsync(
            int? estado, int pagina, int tamanioPagina)
        {
            pagina = Math.Max(1, pagina);
            tamanioPagina = Math.Clamp(tamanioPagina, 1, 50);

            var query = _context.Reclamos.AsNoTracking().AsQueryable();
            if (estado.HasValue)
                query = query.Where(r => r.Estado == (TipoEstadoReclamo)estado.Value);

            return ListarPaginadoAsync(query, pagina, tamanioPagina, esCliente: null);
        }

        private async Task<ResultadoOperacion<PaginacionRespuestaDto<ReclamoListadoDto>>> ListarPaginadoAsync(
            IQueryable<Reclamo> query, int pagina, int tamanioPagina, bool? esCliente)
        {
            try
            {
                var total = await query.CountAsync();
                var items = await query
                    .OrderByDescending(r => r.ReclamoId)
                    .Skip((pagina - 1) * tamanioPagina)
                    .Take(tamanioPagina)
                    .Select(r => new ReclamoListadoDto
                    {
                        ReclamoId = r.ReclamoId,
                        SubordenId = r.SubordenId,
                        NumeroSuborden = r.Suborden.NumeroSuborden,
                        NombreContraparte = esCliente == true
                            ? r.Suborden.Vendedor.NombreTienda
                            : esCliente == false
                                ? r.AbiertoPorUsuario.Persona.Nombres + " " +
                                  (r.AbiertoPorUsuario.Persona.ApellidoPaterno ?? "")
                                : r.Suborden.Vendedor.NombreTienda + " vs " +
                                  r.AbiertoPorUsuario.Persona.Nombres,
                        Motivo = new EnumeracionDto { Id = (int)r.Motivo, Nombre = r.Motivo.ToString() },
                        Estado = new EnumeracionDto { Id = (int)r.Estado, Nombre = r.Estado.ToString() },
                        FechaApertura = r.FechaApertura,
                        TotalMensajes = r.Mensajes.Count
                    })
                    .ToListAsync();

                return ResultadoOperacion<PaginacionRespuestaDto<ReclamoListadoDto>>.SetExito(
                    new PaginacionRespuestaDto<ReclamoListadoDto>
                    {
                        Items = items,
                        Pagina = pagina,
                        TamanioPagina = tamanioPagina,
                        TotalRegistros = total,
                        HayMas = pagina * tamanioPagina < total
                    });
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<PaginacionRespuestaDto<ReclamoListadoDto>>.SetError("Error: " + ex.Message);
            }
        }

        private async Task<bool> TieneAccesoAsync(int usuarioId, Reclamo reclamo)
        {
            if (await EsAdminOVerificadorAsync(usuarioId)) return true;
            if (reclamo.AbiertoPor == usuarioId) return true;
            return reclamo.Suborden.Vendedor.UsuarioId == usuarioId;
        }

        private Task<bool> EsAdminOVerificadorAsync(int usuarioId) =>
            _context.UsuarioRoles.AnyAsync(ur =>
                ur.UsuarioId == usuarioId &&
                (ur.Rol.Nombre == "ADMIN" || ur.Rol.Nombre == "VERIFICADOR"));

        private static ReclamoDto MapearADto(Reclamo r) => new()
        {
            ReclamoId = r.ReclamoId,
            SubordenId = r.SubordenId,
            NumeroSuborden = r.Suborden.NumeroSuborden,
            VendedorId = r.Suborden.VendedorId,
            NombreTienda = r.Suborden.Vendedor.NombreTienda,
            AbiertoPor = r.AbiertoPor,
            NombreCliente = $"{r.AbiertoPorUsuario.Persona.Nombres} {r.AbiertoPorUsuario.Persona.ApellidoPaterno}".Trim(),
            Motivo = new EnumeracionDto { Id = (int)r.Motivo, Nombre = r.Motivo.GetDescription() },
            Descripcion = r.Descripcion,
            Evidencias = string.IsNullOrEmpty(r.Evidencias)
                ? new List<string>()
                : JsonSerializer.Deserialize<List<string>>(r.Evidencias) ?? new List<string>(),
            Estado = new EnumeracionDto { Id = (int)r.Estado, Nombre = r.Estado.GetDescription() },
            NotasResolucion = r.NotasResolucion,
            MontoReembolso = r.MontoReembolso,
            FechaApertura = r.FechaApertura,
            FechaResolucion = r.FechaResolucion,
            Mensajes = r.Mensajes.OrderBy(m => m.MensajeId).Select(m => new MensajeReclamoDto
            {
                MensajeId = m.MensajeId,
                RemitenteId = m.RemitenteId,
                NombreRemitente = $"{m.Remitente.Persona.Nombres} {m.Remitente.Persona.ApellidoPaterno}".Trim(),
                RolRemitente = ObtenerRolPrincipal(m.Remitente),
                Mensaje = m.Mensaje,
                Adjuntos = string.IsNullOrEmpty(m.Adjuntos)
                    ? new List<string>()
                    : JsonSerializer.Deserialize<List<string>>(m.Adjuntos) ?? new List<string>(),
                Fecha = m.Fecha
            }).ToList()
        };

        private static string ObtenerRolPrincipal(Usuario u)
        {
            var roles = u.UsuarioRoles?.Select(ur => ur.Rol.Nombre).ToList() ?? new List<string>();
            if (roles.Contains("ADMIN")) return "ADMIN";
            if (roles.Contains("VERIFICADOR")) return "VERIFICADOR";
            if (roles.Contains("VENDEDOR")) return "VENDEDOR";
            return "CLIENTE";
        }
    }
}
