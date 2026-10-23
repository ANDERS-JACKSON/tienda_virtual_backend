using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TiendaVirtual.Dominio.Modelo.SeguridadXqm;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.SeguridadXqm;

namespace TiendaVirtual.Dominio.Servicios.VentaXqm.Implementacion
{
    /// <summary>
    /// Maneja el CRUD de direcciones del cliente (xqm_seguridad.direccion).
    /// Las direcciones pertenecen a la persona, no al usuario, pero se exponen
    /// a través del usuario autenticado (un usuario = una persona).
    /// </summary>
    public class DireccionServicio : IDireccionServicio
    {
        protected readonly TiendaVirtualDbContext _context;
        private readonly ILogger<DireccionServicio> _logger;

        public DireccionServicio(TiendaVirtualDbContext context, ILogger<DireccionServicio> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ResultadoOperacion<List<DireccionDto>>> ListarMisDireccionesAsync(Guid usuarioId)
        {
            try
            {
                var personaId = await ObtenerPersonaIdAsync(usuarioId);
                if (personaId == null)
                    return ResultadoOperacion<List<DireccionDto>>.SetError("Usuario sin persona asociada.");

                var direcciones = await _context.Direcciones.AsNoTracking()
                    .Where(d => d.PersonaId == personaId)
                    .OrderByDescending(d => d.EsPredeterminada)
                    .ThenByDescending(d => d.DireccionId)
                    .ToListAsync();

                return ResultadoOperacion<List<DireccionDto>>.SetExito(
                    direcciones.Select(MapearDto).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en DireccionServicio.ListarMisDireccionesAsync");
                return ResultadoOperacion<List<DireccionDto>>.SetError("Ocurrió un error inesperado. Intente nuevamente.");
            }

        }

        public async Task<ResultadoOperacion<DireccionDto>> ObtenerPorIdAsync(Guid usuarioId, Guid direccionId)
        {
            try
            {
                var d = await CargarDireccionPropiaAsync(usuarioId, direccionId);
                if (d == null)
                    return ResultadoOperacion<DireccionDto>.SetError("Dirección no encontrada.");

                return ResultadoOperacion<DireccionDto>.SetExito(MapearDto(d));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en DireccionServicio.ObtenerPorIdAsync");
                return ResultadoOperacion<DireccionDto>.SetError("Ocurrió un error inesperado. Intente nuevamente.");
            }

        }

        public async Task<ResultadoOperacion<DireccionDto>> CrearAsync(Guid usuarioId, CrearDireccionDto dto)
        {
            using var trx = await _context.Database.BeginTransactionAsync();
            try
            {
                var personaId = await ObtenerPersonaIdAsync(usuarioId);
                if (personaId == null)
                    return ResultadoOperacion<DireccionDto>.SetError("Usuario sin persona asociada.");

                var datosCuenta = await CompletarReceptorDesdePersonaAsync(personaId.Value, dto);
                if (datosCuenta != null)
                    return ResultadoOperacion<DireccionDto>.SetError(datosCuenta);

                var error = ValidarCampos(dto);
                if (error != null)
                    return ResultadoOperacion<DireccionDto>.SetError(error);

                // La primera dirección siempre es predeterminada.
                var esPrimera = !await _context.Direcciones.AnyAsync(d => d.PersonaId == personaId);

                if (dto.EsPredeterminada || esPrimera)
                    await QuitarPredeterminadaAEsasOtrasAsync(personaId.Value, null);

                var ubicacion = await ResolverUbigeoAsync(dto.DistritoId);
                if (ubicacion == null)
                    return ResultadoOperacion<DireccionDto>.SetError("Distrito ubigeo no válido.");

                var direccion = new Direccion
                {
                    DireccionId = Guid.NewGuid(),
                    PersonaId = personaId.Value,
                    Etiqueta = dto.Etiqueta?.Trim(),
                    NombreReceptor = dto.NombreReceptor!.Trim(),
                    DniReceptor = dto.DniReceptor!.Trim(),
                    Telefono = dto.Telefono?.Trim(),
                    DistritoId = ubicacion.DistritoId,
                    Departamento = ubicacion.Departamento,
                    Provincia = ubicacion.Provincia,
                    Distrito = ubicacion.Distrito,
                    DireccionLinea = dto.DireccionLinea.Trim(),
                    Referencia = dto.Referencia?.Trim(),
                    EsPredeterminada = dto.EsPredeterminada || esPrimera
                };

                _context.Direcciones.Add(direccion);
                await _context.SaveChangesAsync();
                await trx.CommitAsync();

                return ResultadoOperacion<DireccionDto>.SetExito(MapearDto(direccion));
            }
            catch (Exception ex)
            {
                await trx.RollbackAsync();
                _logger.LogError(ex, "Error en DireccionServicio.CrearAsync");
                return ResultadoOperacion<DireccionDto>.SetError("Ocurrió un error inesperado. Intente nuevamente.");
            }

        }

        public async Task<ResultadoOperacion<DireccionDto>> ActualizarAsync(
            Guid usuarioId,             Guid direccionId, ActualizarDireccionDto dto)
        {
            using var trx = await _context.Database.BeginTransactionAsync();
            try
            {
                var d = await CargarDireccionPropiaAsync(usuarioId, direccionId);
                if (d == null)
                    return ResultadoOperacion<DireccionDto>.SetError("Dirección no encontrada.");

                // El formulario del lugar no reenvía al receptor. Si ya hay alguien
                // guardado en esta dirección, se conserva.
                if (string.IsNullOrWhiteSpace(dto.NombreReceptor))
                    dto.NombreReceptor = d.NombreReceptor;
                if (string.IsNullOrWhiteSpace(dto.DniReceptor))
                    dto.DniReceptor = d.DniReceptor;

                var datosCuenta = await CompletarReceptorDesdePersonaAsync(d.PersonaId, dto);
                if (datosCuenta != null)
                    return ResultadoOperacion<DireccionDto>.SetError(datosCuenta);

                var error = ValidarCampos(dto);
                if (error != null)
                    return ResultadoOperacion<DireccionDto>.SetError(error);

                if (dto.EsPredeterminada && !d.EsPredeterminada)
                    await QuitarPredeterminadaAEsasOtrasAsync(d.PersonaId, d.DireccionId);

                var ubicacion = await ResolverUbigeoAsync(dto.DistritoId);
                if (ubicacion == null)
                    return ResultadoOperacion<DireccionDto>.SetError("Distrito ubigeo no válido.");

                d.Etiqueta = dto.Etiqueta?.Trim();
                d.NombreReceptor = dto.NombreReceptor!.Trim();
                d.DniReceptor = dto.DniReceptor!.Trim();
                d.Telefono = dto.Telefono?.Trim();
                d.DistritoId = ubicacion.DistritoId;
                d.Departamento = ubicacion.Departamento;
                d.Provincia = ubicacion.Provincia;
                d.Distrito = ubicacion.Distrito;
                d.DireccionLinea = dto.DireccionLinea.Trim();
                d.Referencia = dto.Referencia?.Trim();
                d.EsPredeterminada = dto.EsPredeterminada;

                await _context.SaveChangesAsync();
                await trx.CommitAsync();

                return ResultadoOperacion<DireccionDto>.SetExito(MapearDto(d));
            }
            catch (Exception ex)
            {
                await trx.RollbackAsync();
                _logger.LogError(ex, "Error en DireccionServicio.ActualizarAsync");
                return ResultadoOperacion<DireccionDto>.SetError("Ocurrió un error inesperado. Intente nuevamente.");
            }

        }

        public async Task<ResultadoOperacion<bool>> EliminarAsync(Guid usuarioId, Guid direccionId)
        {
            using var trx = await _context.Database.BeginTransactionAsync();
            try
            {
                var d = await CargarDireccionPropiaAsync(usuarioId, direccionId);
                if (d == null)
                    return ResultadoOperacion<bool>.SetError("Dirección no encontrada.");

                var eraPredeterminada = d.EsPredeterminada;
                var personaId = d.PersonaId;

                _context.Direcciones.Remove(d);
                await _context.SaveChangesAsync();

                // Si era la predeterminada, promueve la siguiente disponible.
                if (eraPredeterminada)
                {
                    var siguiente = await _context.Direcciones
                        .Where(x => x.PersonaId == personaId)
                        .OrderByDescending(x => x.DireccionId)
                        .FirstOrDefaultAsync();

                    if (siguiente != null)
                    {
                        siguiente.EsPredeterminada = true;
                        await _context.SaveChangesAsync();
                    }
                }

                await trx.CommitAsync();
                return ResultadoOperacion<bool>.SetExito(true);
            }
            catch (Exception ex)
            {
                await trx.RollbackAsync();
                _logger.LogError(ex, "Error en DireccionServicio.EliminarAsync");
                return ResultadoOperacion<bool>.SetError("Ocurrió un error inesperado. Intente nuevamente.");
            }

        }

        public async Task<ResultadoOperacion<bool>> MarcarPredeterminadaAsync(Guid usuarioId, Guid direccionId)
        {
            using var trx = await _context.Database.BeginTransactionAsync();
            try
            {
                var d = await CargarDireccionPropiaAsync(usuarioId, direccionId);
                if (d == null)
                    return ResultadoOperacion<bool>.SetError("Dirección no encontrada.");

                await QuitarPredeterminadaAEsasOtrasAsync(d.PersonaId, d.DireccionId);
                d.EsPredeterminada = true;

                await _context.SaveChangesAsync();
                await trx.CommitAsync();

                return ResultadoOperacion<bool>.SetExito(true);
            }
            catch (Exception ex)
            {
                await trx.RollbackAsync();
                _logger.LogError(ex, "Error en DireccionServicio.MarcarPredeterminadaAsync");
                return ResultadoOperacion<bool>.SetError("Ocurrió un error inesperado. Intente nuevamente.");
            }
        }

        // ─────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────
        private static string? ValidarCampos(CrearDireccionDto dto)
        {
            dto.NombreReceptor = Regex.Replace(dto.NombreReceptor ?? string.Empty, @"\s+", " ").Trim();
            dto.DniReceptor = new string((dto.DniReceptor ?? string.Empty).Where(char.IsDigit).ToArray());
            dto.Telefono = new string((dto.Telefono ?? string.Empty).Where(char.IsDigit).ToArray());
            if (dto.Telefono.StartsWith("51") && dto.Telefono.Length == 11)
                dto.Telefono = dto.Telefono[2..];
            dto.DireccionLinea = (dto.DireccionLinea ?? string.Empty).Trim();
            dto.DistritoId = (dto.DistritoId ?? string.Empty).Trim();
            dto.Etiqueta = string.IsNullOrWhiteSpace(dto.Etiqueta) ? null : dto.Etiqueta.Trim();
            dto.Referencia = string.IsNullOrWhiteSpace(dto.Referencia) ? null : dto.Referencia.Trim();

            if (dto.Telefono.Length != 9 || dto.Telefono[0] != '9')
                return "El celular debe tener 9 dígitos y empezar con 9.";

            if (dto.DistritoId.Length != 6 || dto.DistritoId.Any(c => !char.IsDigit(c)))
                return "Selecciona un distrito.";

            if (dto.DireccionLinea.Length < 8 || !Regex.IsMatch(dto.DireccionLinea, @"\p{L}{3}"))
                return "Ingresa una dirección válida, con calle.";

            if (dto.Etiqueta != null
                && !Regex.IsMatch(dto.Etiqueta, @"^[\p{L}\d][\p{L}\d\s'.\-]{0,48}$"))
                return "La etiqueta solo puede tener letras y números.";

            if (dto.Referencia != null && (dto.Referencia.Length < 3 || !Regex.IsMatch(dto.Referencia, @"\p{L}")))
                return "Escribe una referencia más clara.";

            return null;
        }

        private async Task<string?> CompletarReceptorDesdePersonaAsync(Guid personaId, CrearDireccionDto dto)
        {
            if (!string.IsNullOrWhiteSpace(dto.NombreReceptor) && !string.IsNullOrWhiteSpace(dto.DniReceptor))
                return null;

            var persona = await _context.Personas.AsNoTracking()
                .Where(p => p.PersonaId == personaId)
                .Select(p => new { p.Nombres, p.ApellidoPaterno, p.ApellidoMaterno, p.NumeroDocumento })
                .FirstOrDefaultAsync();
            if (persona == null)
                return "No encontramos los datos de tu cuenta.";

            if (string.IsNullOrWhiteSpace(dto.NombreReceptor))
            {
                dto.NombreReceptor = string.Join(' ', new[] { persona.Nombres, persona.ApellidoPaterno, persona.ApellidoMaterno }
                    .Where(s => !string.IsNullOrWhiteSpace(s)));
            }

            if (string.IsNullOrWhiteSpace(dto.DniReceptor))
                dto.DniReceptor = new string((persona.NumeroDocumento ?? string.Empty).Where(char.IsDigit).ToArray());

            if (string.IsNullOrWhiteSpace(dto.NombreReceptor) || string.IsNullOrWhiteSpace(dto.DniReceptor))
                return "Completa tu nombre y documento en tu cuenta antes de guardar una dirección.";

            return null;
        }

        private async Task<Guid?> ObtenerPersonaIdAsync(Guid usuarioId)
        {
            return await _context.Usuarios
                .Where(u => u.UsuarioId == usuarioId)
                .Select(u => (Guid?)u.PersonaId)
                .FirstOrDefaultAsync();
        }

        private async Task<Direccion?> CargarDireccionPropiaAsync(Guid usuarioId, Guid direccionId)
        {
            var personaId = await ObtenerPersonaIdAsync(usuarioId);
            if (personaId == null) return null;

            return await _context.Direcciones.FirstOrDefaultAsync(
                d => d.DireccionId == direccionId && d.PersonaId == personaId);
        }

        private async Task QuitarPredeterminadaAEsasOtrasAsync(Guid personaId, Guid? exceptoDireccionId)
        {
            var otras = await _context.Direcciones
                .Where(x => x.PersonaId == personaId
                            && x.EsPredeterminada
                            && (exceptoDireccionId == null || x.DireccionId != exceptoDireccionId))
                .ToListAsync();

            foreach (var o in otras) o.EsPredeterminada = false;
        }

        private async Task<UbicacionResuelta?> ResolverUbigeoAsync(string? distritoId)
        {
            var id = (distritoId ?? string.Empty).Trim();
            if (id.Length != 6) return null;

            return await (
                from di in _context.Distritos.AsNoTracking()
                join pr in _context.Provincias.AsNoTracking() on di.ProvinciaId equals pr.ProvinciaId
                join de in _context.Departamentos.AsNoTracking() on pr.DepartamentoId equals de.DepartamentoId
                where di.DistritoId == id
                select new UbicacionResuelta(
                    di.DistritoId,
                    de.Nombre,
                    pr.Nombre,
                    di.Nombre)
            ).FirstOrDefaultAsync();
        }

        private sealed record UbicacionResuelta(
            string DistritoId,
            string Departamento,
            string Provincia,
            string Distrito);

        private static DireccionDto MapearDto(Direccion d) => new()
        {
            DireccionId = d.DireccionId,
            PersonaId = d.PersonaId,
            Etiqueta = d.Etiqueta,
            NombreReceptor = d.NombreReceptor,
            DniReceptor = d.DniReceptor,
            Telefono = d.Telefono,
            DistritoId = d.DistritoId,
            Departamento = d.Departamento,
            Provincia = d.Provincia,
            Distrito = d.Distrito,
            DireccionLinea = d.DireccionLinea,
            Referencia = d.Referencia,
            EsPredeterminada = d.EsPredeterminada
        };
    }
}
