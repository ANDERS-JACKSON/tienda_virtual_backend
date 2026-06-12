using Microsoft.EntityFrameworkCore;
using TiendaVirtual.Dominio.Modelo.VentaXqm;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.VentaXqm;

namespace TiendaVirtual.Dominio.Servicios.VentaXqm.Implementacion
{
    public partial class MetodoEnvioServicio
    {
        public async Task<ResultadoOperacion<List<MetodoEnvioAdminDto>>> ListarTodosAdminAsync()
        {
            try
            {
                var metodos = await _context.MetodosEnvio.AsNoTracking()
                    .OrderBy(m => m.Orden).ThenBy(m => m.Nombre).ToListAsync();
                return ResultadoOperacion<List<MetodoEnvioAdminDto>>.SetExito(metodos.Select(MapearAdmin).ToList());
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<List<MetodoEnvioAdminDto>>.SetError("Error: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<MetodoEnvioAdminDto>> CrearAsync(CrearMetodoEnvioDto dto)
        {
            try
            {
                if (dto == null) return ResultadoOperacion<MetodoEnvioAdminDto>.SetError("Datos inválidos.");
                var codigo = dto.Codigo.Trim().ToUpper();
                if (await _context.MetodosEnvio.AnyAsync(m => m.Codigo == codigo))
                    return ResultadoOperacion<MetodoEnvioAdminDto>.SetError("Ya existe un método con ese código.");

                var m = new MetodoEnvio
                {
                    Codigo = codigo,
                    Nombre = dto.Nombre.Trim(),
                    Descripcion = dto.Descripcion?.Trim(),
                    MontoBase = dto.CostoBase,
                    TiempoEstimadoDias = dto.DiasEntregaMax,
                    Activo = true,
                    Orden = await _context.MetodosEnvio.CountAsync() + 1
                };
                _context.MetodosEnvio.Add(m);
                await _context.SaveChangesAsync();
                return ResultadoOperacion<MetodoEnvioAdminDto>.SetExito(MapearAdmin(m));
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<MetodoEnvioAdminDto>.SetError("Error: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<MetodoEnvioAdminDto>> ActualizarAsync(int id, ActualizarMetodoEnvioDto dto)
        {
            try
            {
                if (dto == null) return ResultadoOperacion<MetodoEnvioAdminDto>.SetError("Datos inválidos.");
                var m = await _context.MetodosEnvio.FindAsync(id);
                if (m == null) return ResultadoOperacion<MetodoEnvioAdminDto>.SetError("Método no encontrado.");

                var codigoNuevo = dto.Codigo.Trim().ToUpper();
                if (codigoNuevo != m.Codigo)
                {
                    if (await _context.Subordenes.AnyAsync(s => s.MetodoEnvioId == id))
                        return ResultadoOperacion<MetodoEnvioAdminDto>.SetError(
                            "No se puede cambiar el código porque hay órdenes asociadas.");
                    if (await _context.MetodosEnvio.AnyAsync(x => x.Codigo == codigoNuevo && x.MetodoEnvioId != id))
                        return ResultadoOperacion<MetodoEnvioAdminDto>.SetError("Ya existe un método con ese código.");
                    m.Codigo = codigoNuevo;
                }

                m.Nombre = dto.Nombre.Trim();
                m.Descripcion = dto.Descripcion?.Trim();
                m.MontoBase = dto.CostoBase;
                m.TiempoEstimadoDias = dto.DiasEntregaMax;
                m.Activo = dto.Activo;
                await _context.SaveChangesAsync();
                return ResultadoOperacion<MetodoEnvioAdminDto>.SetExito(MapearAdmin(m));
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<MetodoEnvioAdminDto>.SetError("Error: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<bool>> ActivarAsync(int id)
        {
            try
            {
                var m = await _context.MetodosEnvio.FindAsync(id);
                if (m == null) return ResultadoOperacion<bool>.SetError("Método no encontrado.");
                m.Activo = true;
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
                var activos = await _context.MetodosEnvio.CountAsync(m => m.Activo);
                var m = await _context.MetodosEnvio.FindAsync(id);
                if (m == null) return ResultadoOperacion<bool>.SetError("Método no encontrado.");
                if (m.Activo && activos <= 1)
                    return ResultadoOperacion<bool>.SetError("Debe existir al menos un método de envío activo.");

                m.Activo = false;
                await _context.SaveChangesAsync();
                return ResultadoOperacion<bool>.SetExito(true);
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<bool>.SetError("Error: " + ex.Message);
            }
        }

        private static MetodoEnvioAdminDto MapearAdmin(MetodoEnvio m) => new()
        {
            MetodoEnvioId = m.MetodoEnvioId,
            Codigo = m.Codigo,
            Nombre = m.Nombre,
            Descripcion = m.Descripcion,
            CostoBase = m.MontoBase,
            DiasEntregaMin = m.TiempoEstimadoDias,
            DiasEntregaMax = m.TiempoEstimadoDias,
            Activo = m.Activo
        };
    }
}
