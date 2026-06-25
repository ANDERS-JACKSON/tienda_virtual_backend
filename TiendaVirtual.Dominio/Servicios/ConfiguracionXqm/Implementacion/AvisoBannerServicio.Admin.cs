using Microsoft.EntityFrameworkCore;
using TiendaVirtual.Dominio.Modelo.ConfiguracionXqm;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.ConfiguracionXqm;

namespace TiendaVirtual.Dominio.Servicios.ConfiguracionXqm.Implementacion
{
    public partial class AvisoBannerServicio
    {
        public async Task<ResultadoOperacion<List<AvisoBannerAdminDto>>> ListarTodosAdminAsync()
        {
            try
            {
                var avisos = await _context.AvisosBanner.AsNoTracking()
                    .OrderBy(a => a.Orden)
                    .ThenBy(a => a.AvisoBannerId)
                    .ToListAsync();

                return ResultadoOperacion<List<AvisoBannerAdminDto>>.SetExito(
                    avisos.Select(MapearAdmin).ToList());
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<List<AvisoBannerAdminDto>>.SetError("Error: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<AvisoBannerAdminDto>> CrearAsync(CrearAvisoBannerDto dto)
        {
            try
            {
                if (dto == null || string.IsNullOrWhiteSpace(dto.Texto))
                    return ResultadoOperacion<AvisoBannerAdminDto>.SetError("El texto del aviso es obligatorio.");

                var maxOrden = await _context.AvisosBanner.MaxAsync(a => (int?)a.Orden) ?? 0;
                var aviso = new AvisoBanner
                {
                    Texto = dto.Texto.Trim(),
                    Activo = true,
                    Orden = maxOrden + 1
                };

                _context.AvisosBanner.Add(aviso);
                await _context.SaveChangesAsync();
                return ResultadoOperacion<AvisoBannerAdminDto>.SetExito(MapearAdmin(aviso));
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<AvisoBannerAdminDto>.SetError("Error: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<AvisoBannerAdminDto>> ActualizarAsync(
            int id, ActualizarAvisoBannerDto dto)
        {
            try
            {
                if (dto == null || string.IsNullOrWhiteSpace(dto.Texto))
                    return ResultadoOperacion<AvisoBannerAdminDto>.SetError("El texto del aviso es obligatorio.");

                var aviso = await _context.AvisosBanner.FindAsync(id);
                if (aviso == null)
                    return ResultadoOperacion<AvisoBannerAdminDto>.SetError("Aviso no encontrado.");

                aviso.Texto = dto.Texto.Trim();
                aviso.Activo = dto.Activo;
                await _context.SaveChangesAsync();
                return ResultadoOperacion<AvisoBannerAdminDto>.SetExito(MapearAdmin(aviso));
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<AvisoBannerAdminDto>.SetError("Error: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<bool>> ActivarAsync(int id)
        {
            try
            {
                var aviso = await _context.AvisosBanner.FindAsync(id);
                if (aviso == null) return ResultadoOperacion<bool>.SetError("Aviso no encontrado.");
                aviso.Activo = true;
                await _context.SaveChangesAsync();
                return ResultadoOperacion<bool>.SetExito(true);
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<bool>.SetError("Error: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<bool>> DesactivarAsync(int id)
        {
            try
            {
                var aviso = await _context.AvisosBanner.FindAsync(id);
                if (aviso == null) return ResultadoOperacion<bool>.SetError("Aviso no encontrado.");
                aviso.Activo = false;
                await _context.SaveChangesAsync();
                return ResultadoOperacion<bool>.SetExito(true);
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<bool>.SetError("Error: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<bool>> EliminarAsync(int id)
        {
            try
            {
                var aviso = await _context.AvisosBanner.FindAsync(id);
                if (aviso == null) return ResultadoOperacion<bool>.SetError("Aviso no encontrado.");

                _context.AvisosBanner.Remove(aviso);
                await _context.SaveChangesAsync();
                return ResultadoOperacion<bool>.SetExito(true);
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<bool>.SetError("Error: " + ex.Message);
            }
        }

        private static AvisoBannerAdminDto MapearAdmin(AvisoBanner a) => new()
        {
            AvisoBannerId = a.AvisoBannerId,
            Texto = a.Texto,
            Activo = a.Activo,
            Orden = a.Orden
        };
    }
}
