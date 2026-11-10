using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TiendaVirtual.Dominio.Modelo.ConfiguracionXqm;
using TiendaVirtual.Dominio.Utilidad;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.ConfiguracionXqm;

namespace TiendaVirtual.Dominio.Servicios.ConfiguracionXqm.Implementacion
{
    public class FranjaInicioServicio : IFranjaInicioServicio
    {
        private const string EnlaceInvalido =
            "El enlace debe ser una ruta interna (/catalogo/...) o una URL https.";

        private readonly TiendaVirtualDbContext _context;
        private readonly ILogger<FranjaInicioServicio> _logger;

        public FranjaInicioServicio(
            TiendaVirtualDbContext context,
            ILogger<FranjaInicioServicio> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ResultadoOperacion<List<FranjaInicioDto>>> ListarActivosAsync(string zona)
        {
            try
            {
                if (!FranjaInicio.EsZonaValida(zona))
                    return ResultadoOperacion<List<FranjaInicioDto>>.SetError("Zona no válida.");

                var items = await _context.FranjasInicio.AsNoTracking()
                    .Where(f => f.Activo && f.Zona == zona)
                    .OrderBy(f => f.Orden)
                    .ThenBy(f => f.FranjaInicioId)
                    .Select(f => new FranjaInicioDto
                    {
                        FranjaInicioId = f.FranjaInicioId,
                        Zona = f.Zona,
                        ImagenUrl = f.ImagenUrl,
                        Titulo = f.Titulo,
                        EnlaceUrl = f.EnlaceUrl,
                        Orden = f.Orden,
                    })
                    .ToListAsync();

                return ResultadoOperacion<List<FranjaInicioDto>>.SetExito(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listando franjas de inicio públicas");
                return ResultadoOperacion<List<FranjaInicioDto>>.SetError(
                    "Ocurrió un error al obtener las franjas.");
            }
        }

        public async Task<ResultadoOperacion<List<FranjaInicioAdminDto>>> ListarTodosAdminAsync(string? zona)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(zona) && !FranjaInicio.EsZonaValida(zona))
                    return ResultadoOperacion<List<FranjaInicioAdminDto>>.SetError("Zona no válida.");

                var query = _context.FranjasInicio.AsNoTracking().AsQueryable();
                if (!string.IsNullOrWhiteSpace(zona))
                    query = query.Where(f => f.Zona == zona);

                var items = await query
                    .OrderBy(f => f.Zona)
                    .ThenBy(f => f.Orden)
                    .ThenBy(f => f.FranjaInicioId)
                    .ToListAsync();

                return ResultadoOperacion<List<FranjaInicioAdminDto>>.SetExito(
                    items.Select(MapearAdmin).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listando franjas de inicio admin");
                return ResultadoOperacion<List<FranjaInicioAdminDto>>.SetError(
                    "Ocurrió un error al obtener las franjas.");
            }
        }

        public async Task<ResultadoOperacion<FranjaInicioAdminDto>> CrearAsync(CrearFranjaInicioDto dto)
        {
            try
            {
                if (!FranjaInicio.EsZonaValida(dto.Zona))
                    return ResultadoOperacion<FranjaInicioAdminDto>.SetError("Zona no válida.");
                if (!IdentificadorMedia.EsValido(dto.ImagenUrl))
                    return ResultadoOperacion<FranjaInicioAdminDto>.SetError(IdentificadorMedia.MensajeError);
                if (!EsEnlaceValido(dto.EnlaceUrl))
                    return ResultadoOperacion<FranjaInicioAdminDto>.SetError(EnlaceInvalido);

                var total = await _context.FranjasInicio.CountAsync(f => f.Zona == dto.Zona);
                if (total >= FranjaInicio.MaxPorZona)
                    return ResultadoOperacion<FranjaInicioAdminDto>.SetError(
                        $"Solo se permiten {FranjaInicio.MaxPorZona} tarjetas en esta zona.");

                var maxOrden = await _context.FranjasInicio
                    .Where(f => f.Zona == dto.Zona)
                    .MaxAsync(f => (int?)f.Orden) ?? 0;

                var entidad = new FranjaInicio
                {
                    Zona = dto.Zona,
                    ImagenUrl = dto.ImagenUrl.Trim(),
                    Titulo = NormalizarTitulo(dto.Titulo),
                    EnlaceUrl = dto.EnlaceUrl.Trim(),
                    Activo = dto.Activo,
                    Orden = dto.Orden ?? maxOrden + 1,
                };

                _context.FranjasInicio.Add(entidad);
                await _context.SaveChangesAsync();
                return ResultadoOperacion<FranjaInicioAdminDto>.SetExito(MapearAdmin(entidad));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando franja de inicio");
                return ResultadoOperacion<FranjaInicioAdminDto>.SetError(
                    "Ocurrió un error al crear la tarjeta.");
            }
        }

        public async Task<ResultadoOperacion<FranjaInicioAdminDto>> ActualizarAsync(
            int id, ActualizarFranjaInicioDto dto)
        {
            try
            {
                if (!IdentificadorMedia.EsValido(dto.ImagenUrl))
                    return ResultadoOperacion<FranjaInicioAdminDto>.SetError(IdentificadorMedia.MensajeError);
                if (!EsEnlaceValido(dto.EnlaceUrl))
                    return ResultadoOperacion<FranjaInicioAdminDto>.SetError(EnlaceInvalido);

                var entidad = await _context.FranjasInicio.FindAsync(id);
                if (entidad == null)
                    return ResultadoOperacion<FranjaInicioAdminDto>.SetError("Tarjeta no encontrada.");

                entidad.ImagenUrl = dto.ImagenUrl.Trim();
                entidad.Titulo = NormalizarTitulo(dto.Titulo);
                entidad.EnlaceUrl = dto.EnlaceUrl.Trim();
                entidad.Activo = dto.Activo;
                entidad.Orden = dto.Orden;
                await _context.SaveChangesAsync();
                return ResultadoOperacion<FranjaInicioAdminDto>.SetExito(MapearAdmin(entidad));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando franja de inicio {Id}", id);
                return ResultadoOperacion<FranjaInicioAdminDto>.SetError(
                    "Ocurrió un error al actualizar la tarjeta.");
            }
        }

        public async Task<ResultadoOperacion<bool>> EliminarAsync(int id)
        {
            try
            {
                var entidad = await _context.FranjasInicio.FindAsync(id);
                if (entidad == null)
                    return ResultadoOperacion<bool>.SetError("Tarjeta no encontrada.");

                _context.FranjasInicio.Remove(entidad);
                await _context.SaveChangesAsync();
                return ResultadoOperacion<bool>.SetExito(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando franja de inicio {Id}", id);
                return ResultadoOperacion<bool>.SetError("Ocurrió un error al eliminar la tarjeta.");
            }
        }

        private static FranjaInicioAdminDto MapearAdmin(FranjaInicio f) => new()
        {
            FranjaInicioId = f.FranjaInicioId,
            Zona = f.Zona,
            ImagenUrl = f.ImagenUrl,
            Titulo = f.Titulo,
            EnlaceUrl = f.EnlaceUrl,
            Activo = f.Activo,
            Orden = f.Orden,
        };

        private static string? NormalizarTitulo(string? titulo)
        {
            var t = titulo?.Trim();
            return string.IsNullOrEmpty(t) ? null : t;
        }

        internal static bool EsEnlaceValido(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            var v = url.Trim();
            if (v.Length > 500) return false;

            if (v.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return Uri.TryCreate(v, UriKind.Absolute, out var uri)
                    && uri.Scheme == Uri.UriSchemeHttps;

            return v.StartsWith('/')
                && !v.StartsWith("//", StringComparison.Ordinal)
                && !v.Contains('\\')
                && v.IndexOf(':') < 0;
        }
    }
}
