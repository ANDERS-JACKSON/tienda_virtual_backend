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
    public class CarruselInicioServicio : ICarruselInicioServicio
    {
        private const int MaxSlides = 12;
        private readonly TiendaVirtualDbContext _context;
        private readonly ILogger<CarruselInicioServicio> _logger;

        public CarruselInicioServicio(
            TiendaVirtualDbContext context,
            ILogger<CarruselInicioServicio> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ResultadoOperacion<List<CarruselInicioDto>>> ListarActivosAsync()
        {
            try
            {
                var items = await _context.CarruselesInicio.AsNoTracking()
                    .Where(c => c.Activo)
                    .OrderBy(c => c.Orden)
                    .ThenBy(c => c.CarruselInicioId)
                    .Select(c => new CarruselInicioDto
                    {
                        CarruselInicioId = c.CarruselInicioId,
                        ImagenUrl = c.ImagenUrl,
                        Titulo = c.Titulo,
                        Subtitulo = c.Subtitulo,
                        EnlaceUrl = c.EnlaceUrl,
                        TextoBoton = c.TextoBoton,
                        Orden = c.Orden
                    })
                    .ToListAsync();

                return ResultadoOperacion<List<CarruselInicioDto>>.SetExito(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listando carrusel de inicio público");
                return ResultadoOperacion<List<CarruselInicioDto>>.SetError(
                    "Ocurrió un error al obtener el carrusel.");
            }
        }

        public async Task<ResultadoOperacion<List<CarruselInicioAdminDto>>> ListarTodosAdminAsync()
        {
            try
            {
                var items = await _context.CarruselesInicio.AsNoTracking()
                    .OrderBy(c => c.Orden)
                    .ThenBy(c => c.CarruselInicioId)
                    .ToListAsync();

                return ResultadoOperacion<List<CarruselInicioAdminDto>>.SetExito(
                    items.Select(MapearAdmin).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listando carrusel de inicio admin");
                return ResultadoOperacion<List<CarruselInicioAdminDto>>.SetError(
                    "Ocurrió un error al obtener el carrusel.");
            }
        }

        public async Task<ResultadoOperacion<CarruselInicioAdminDto>> CrearAsync(CrearCarruselInicioDto dto)
        {
            try
            {
                if (dto == null || string.IsNullOrWhiteSpace(dto.ImagenUrl))
                    return ResultadoOperacion<CarruselInicioAdminDto>.SetError(
                        "La imagen es obligatoria.");
                if (!IdentificadorMedia.EsValido(dto.ImagenUrl))
                    return ResultadoOperacion<CarruselInicioAdminDto>.SetError(IdentificadorMedia.MensajeError);

                var total = await _context.CarruselesInicio.CountAsync();
                if (total >= MaxSlides)
                    return ResultadoOperacion<CarruselInicioAdminDto>.SetError(
                        $"Solo se permiten {MaxSlides} imágenes en el carrusel.");

                var maxOrden = await _context.CarruselesInicio.MaxAsync(c => (int?)c.Orden) ?? 0;
                var entidad = new CarruselInicio
                {
                    ImagenUrl = dto.ImagenUrl.Trim(),
                    Titulo = NormalizarOpcional(dto.Titulo, 120),
                    Subtitulo = NormalizarOpcional(dto.Subtitulo, 300),
                    EnlaceUrl = NormalizarOpcional(dto.EnlaceUrl, 500),
                    TextoBoton = NormalizarOpcional(dto.TextoBoton, 80),
                    Activo = dto.Activo,
                    Orden = dto.Orden ?? (maxOrden + 1)
                };

                _context.CarruselesInicio.Add(entidad);
                await _context.SaveChangesAsync();
                return ResultadoOperacion<CarruselInicioAdminDto>.SetExito(MapearAdmin(entidad));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando slide de carrusel");
                return ResultadoOperacion<CarruselInicioAdminDto>.SetError(
                    "Ocurrió un error al crear la imagen del carrusel.");
            }
        }

        public async Task<ResultadoOperacion<CarruselInicioAdminDto>> ActualizarAsync(
            int id, ActualizarCarruselInicioDto dto)
        {
            try
            {
                if (dto == null || string.IsNullOrWhiteSpace(dto.ImagenUrl))
                    return ResultadoOperacion<CarruselInicioAdminDto>.SetError(
                        "La imagen es obligatoria.");
                if (!IdentificadorMedia.EsValido(dto.ImagenUrl))
                    return ResultadoOperacion<CarruselInicioAdminDto>.SetError(IdentificadorMedia.MensajeError);

                var entidad = await _context.CarruselesInicio.FindAsync(id);
                if (entidad == null)
                    return ResultadoOperacion<CarruselInicioAdminDto>.SetError(
                        "Imagen del carrusel no encontrada.");

                entidad.ImagenUrl = dto.ImagenUrl.Trim();
                entidad.Titulo = NormalizarOpcional(dto.Titulo, 120);
                entidad.Subtitulo = NormalizarOpcional(dto.Subtitulo, 300);
                entidad.EnlaceUrl = NormalizarOpcional(dto.EnlaceUrl, 500);
                entidad.TextoBoton = NormalizarOpcional(dto.TextoBoton, 80);
                entidad.Activo = dto.Activo;
                entidad.Orden = dto.Orden;

                await _context.SaveChangesAsync();
                return ResultadoOperacion<CarruselInicioAdminDto>.SetExito(MapearAdmin(entidad));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando slide de carrusel {Id}", id);
                return ResultadoOperacion<CarruselInicioAdminDto>.SetError(
                    "Ocurrió un error al actualizar la imagen del carrusel.");
            }
        }

        public async Task<ResultadoOperacion<bool>> ActivarAsync(int id)
        {
            try
            {
                var entidad = await _context.CarruselesInicio.FindAsync(id);
                if (entidad == null)
                    return ResultadoOperacion<bool>.SetError("Imagen del carrusel no encontrada.");
                entidad.Activo = true;
                await _context.SaveChangesAsync();
                return ResultadoOperacion<bool>.SetExito(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activando slide de carrusel {Id}", id);
                return ResultadoOperacion<bool>.SetError("Ocurrió un error al activar la imagen.");
            }
        }

        public async Task<ResultadoOperacion<bool>> DesactivarAsync(int id)
        {
            try
            {
                var entidad = await _context.CarruselesInicio.FindAsync(id);
                if (entidad == null)
                    return ResultadoOperacion<bool>.SetError("Imagen del carrusel no encontrada.");
                entidad.Activo = false;
                await _context.SaveChangesAsync();
                return ResultadoOperacion<bool>.SetExito(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error desactivando slide de carrusel {Id}", id);
                return ResultadoOperacion<bool>.SetError("Ocurrió un error al desactivar la imagen.");
            }
        }

        public async Task<ResultadoOperacion<bool>> EliminarAsync(int id)
        {
            try
            {
                var entidad = await _context.CarruselesInicio.FindAsync(id);
                if (entidad == null)
                    return ResultadoOperacion<bool>.SetError("Imagen del carrusel no encontrada.");

                _context.CarruselesInicio.Remove(entidad);
                await _context.SaveChangesAsync();
                return ResultadoOperacion<bool>.SetExito(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando slide de carrusel {Id}", id);
                return ResultadoOperacion<bool>.SetError("Ocurrió un error al eliminar la imagen.");
            }
        }

        private static string? NormalizarOpcional(string? valor, int max)
        {
            if (string.IsNullOrWhiteSpace(valor)) return null;
            var t = valor.Trim();
            return t.Length <= max ? t : t[..max];
        }

        private static CarruselInicioAdminDto MapearAdmin(CarruselInicio c) => new()
        {
            CarruselInicioId = c.CarruselInicioId,
            ImagenUrl = c.ImagenUrl,
            Titulo = c.Titulo,
            Subtitulo = c.Subtitulo,
            EnlaceUrl = c.EnlaceUrl,
            TextoBoton = c.TextoBoton,
            Activo = c.Activo,
            Orden = c.Orden
        };
    }
}
