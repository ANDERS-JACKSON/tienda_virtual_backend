using System;
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

        public AvisoBannerServicio(TiendaVirtualDbContext context) => _context = context;

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
                return ResultadoOperacion<List<AvisoBannerDto>>.SetError("Error: " + ex.Message);
            }
        }
    }
}
