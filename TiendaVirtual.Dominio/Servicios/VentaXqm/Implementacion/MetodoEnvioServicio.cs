using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.VentaXqm;

namespace TiendaVirtual.Dominio.Servicios.VentaXqm.Implementacion
{
    public class MetodoEnvioServicio : IMetodoEnvioServicio
    {
        protected readonly TiendaVirtualDbContext _context;

        public MetodoEnvioServicio(TiendaVirtualDbContext context) => _context = context;

        public async Task<ResultadoOperacion<List<MetodoEnvioDto>>> ListarActivosAsync()
        {
            try
            {
                var metodos = await _context.MetodosEnvio.AsNoTracking()
                    .Where(m => m.Activo)
                    .OrderBy(m => m.Orden)
                    .ThenBy(m => m.MontoBase)
                    .ToListAsync();

                var dtos = metodos.Select(m => new MetodoEnvioDto
                {
                    MetodoEnvioId = m.MetodoEnvioId,
                    Codigo = m.Codigo,
                    Nombre = m.Nombre,
                    Descripcion = m.Descripcion,
                    MontoBase = m.MontoBase,
                    TiempoEstimadoDias = m.TiempoEstimadoDias
                }).ToList();

                return ResultadoOperacion<List<MetodoEnvioDto>>.SetExito(dtos);
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<List<MetodoEnvioDto>>.SetError("Error: " + ex.Message);
            }
        }
    }
}
