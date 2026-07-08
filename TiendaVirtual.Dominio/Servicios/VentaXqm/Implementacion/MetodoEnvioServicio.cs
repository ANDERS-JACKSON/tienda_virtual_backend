using System;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.VentaXqm;

namespace TiendaVirtual.Dominio.Servicios.VentaXqm.Implementacion
{
    public partial class MetodoEnvioServicio : IMetodoEnvioServicio
    {
        protected readonly TiendaVirtualDbContext _context;
        private readonly ILogger<MetodoEnvioServicio> _logger;

        public MetodoEnvioServicio(TiendaVirtualDbContext context, ILogger<MetodoEnvioServicio> logger)
        {
            _context = context;
            _logger = logger;
        }

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
                _logger.LogError(ex, "Error en MetodoEnvioServicio.ListarActivosAsync");
                return ResultadoOperacion<List<MetodoEnvioDto>>.SetError("Ocurrió un error inesperado. Intente nuevamente.");
            }
        }
    }
}
