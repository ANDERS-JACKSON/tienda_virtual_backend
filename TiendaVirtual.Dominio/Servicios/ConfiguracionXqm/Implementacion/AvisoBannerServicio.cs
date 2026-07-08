using System;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TiendaVirtual.Dominio.Modelo.ConfiguracionXqm;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.ConfiguracionXqm;

namespace TiendaVirtual.Dominio.Servicios.ConfiguracionXqm.Implementacion
{
    public partial class AvisoBannerServicio : IAvisoBannerServicio
    {
        protected readonly TiendaVirtualDbContext _context;
        private readonly ILogger<AvisoBannerServicio> _logger;

        public AvisoBannerServicio(TiendaVirtualDbContext context, ILogger<AvisoBannerServicio> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ResultadoOperacion<List<AvisoBannerDto>>> ListarActivosAsync()
        {
            try
            {
                var avisos = await _context.AvisosBanner.AsNoTracking()
                    .Where(a => a.Activo)
                    .OrderBy(a => a.Orden)
                    .ThenBy(a => a.AvisoBannerId)
                    .Select(a => new AvisoBannerDto
                    {
                        AvisoBannerId = a.AvisoBannerId,
                        Texto = a.Texto
                    })
                    .ToListAsync();

                return ResultadoOperacion<List<AvisoBannerDto>>.SetExito(avisos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en AvisoBannerServicio.ListarActivosAsync");
                return ResultadoOperacion<List<AvisoBannerDto>>.SetError("Ocurrió un error inesperado. Intente nuevamente.");
            }
        }
    }
}
