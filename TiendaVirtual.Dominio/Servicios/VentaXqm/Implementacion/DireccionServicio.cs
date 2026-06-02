using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
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

        public DireccionServicio(TiendaVirtualDbContext context) => _context = context;

        public async Task<ResultadoOperacion<List<DireccionDto>>> ListarMisDireccionesAsync(int usuarioId)
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
                return ResultadoOperacion<List<DireccionDto>>.SetError("Error: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<DireccionDto>> ObtenerPorIdAsync(int usuarioId, int direccionId)
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
                return ResultadoOperacion<DireccionDto>.SetError("Error: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<DireccionDto>> CrearAsync(int usuarioId, CrearDireccionDto dto)
        {
            using var trx = await _context.Database.BeginTransactionAsync();
            try
            {
                var personaId = await ObtenerPersonaIdAsync(usuarioId);
                if (personaId == null)
                    return ResultadoOperacion<DireccionDto>.SetError("Usuario sin persona asociada.");

                // La primera dirección siempre es predeterminada.
                var esPrimera = !await _context.Direcciones.AnyAsync(d => d.PersonaId == personaId);

                if (dto.EsPredeterminada || esPrimera)
                    await QuitarPredeterminadaAEsasOtrasAsync(personaId.Value, null);

                var direccion = new Direccion
                {
                    PersonaId = personaId.Value,
                    Etiqueta = dto.Etiqueta?.Trim(),
                    NombreReceptor = dto.NombreReceptor.Trim(),
                    Telefono = dto.Telefono?.Trim(),
                    Departamento = dto.Departamento.Trim(),
                    Provincia = dto.Provincia.Trim(),
                    Distrito = dto.Distrito.Trim(),
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
                return ResultadoOperacion<DireccionDto>.SetError("Error: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<DireccionDto>> ActualizarAsync(
            int usuarioId, int direccionId, ActualizarDireccionDto dto)
        {
            using var trx = await _context.Database.BeginTransactionAsync();
            try
            {
                var d = await CargarDireccionPropiaAsync(usuarioId, direccionId);
                if (d == null)
                    return ResultadoOperacion<DireccionDto>.SetError("Dirección no encontrada.");

                if (dto.EsPredeterminada && !d.EsPredeterminada)
                    await QuitarPredeterminadaAEsasOtrasAsync(d.PersonaId, d.DireccionId);

                d.Etiqueta = dto.Etiqueta?.Trim();
                d.NombreReceptor = dto.NombreReceptor.Trim();
                d.Telefono = dto.Telefono?.Trim();
                d.Departamento = dto.Departamento.Trim();
                d.Provincia = dto.Provincia.Trim();
                d.Distrito = dto.Distrito.Trim();
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
                return ResultadoOperacion<DireccionDto>.SetError("Error: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<bool>> EliminarAsync(int usuarioId, int direccionId)
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
                return ResultadoOperacion<bool>.SetError("Error: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<bool>> MarcarPredeterminadaAsync(int usuarioId, int direccionId)
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
                return ResultadoOperacion<bool>.SetError("Error: " + ex.Message);
            }
        }

        // ─────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────
        private async Task<int?> ObtenerPersonaIdAsync(int usuarioId)
        {
            return await _context.Usuarios
                .Where(u => u.UsuarioId == usuarioId)
                .Select(u => (int?)u.PersonaId)
                .FirstOrDefaultAsync();
        }

        private async Task<Direccion?> CargarDireccionPropiaAsync(int usuarioId, int direccionId)
        {
            var personaId = await ObtenerPersonaIdAsync(usuarioId);
            if (personaId == null) return null;

            return await _context.Direcciones.FirstOrDefaultAsync(
                d => d.DireccionId == direccionId && d.PersonaId == personaId);
        }

        private async Task QuitarPredeterminadaAEsasOtrasAsync(int personaId, int? exceptoDireccionId)
        {
            var otras = await _context.Direcciones
                .Where(x => x.PersonaId == personaId
                            && x.EsPredeterminada
                            && (exceptoDireccionId == null || x.DireccionId != exceptoDireccionId))
                .ToListAsync();

            foreach (var o in otras) o.EsPredeterminada = false;
        }

        private static DireccionDto MapearDto(Direccion d) => new()
        {
            DireccionId = d.DireccionId,
            PersonaId = d.PersonaId,
            Etiqueta = d.Etiqueta,
            NombreReceptor = d.NombreReceptor,
            Telefono = d.Telefono,
            Departamento = d.Departamento,
            Provincia = d.Provincia,
            Distrito = d.Distrito,
            DireccionLinea = d.DireccionLinea,
            Referencia = d.Referencia,
            EsPredeterminada = d.EsPredeterminada
        };
    }
}
