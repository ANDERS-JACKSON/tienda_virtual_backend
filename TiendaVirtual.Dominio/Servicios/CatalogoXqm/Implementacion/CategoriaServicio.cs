using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Extensiones.CatalogoXqm;
using TiendaVirtual.Dominio.Modelo.CatalogoXqm;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.CatalogoXqm;

namespace TiendaVirtual.Dominio.Servicios.CatalogoXqm.Implementacion
{
    public class CategoriaServicio : ICategoriaServicio
    {
        protected readonly TiendaVirtualDbContext _context;

        public CategoriaServicio(TiendaVirtualDbContext context) => _context = context;

        public async Task<ResultadoOperacion<List<CategoriaDto>>> ListarAsync(bool soloActivas = true)
        {
            try
            {
                var query = _context.Categorias.AsNoTracking();
                if (soloActivas) query = query.Where(c => c.Activa);

                var categorias = await query.OrderBy(c => c.Orden).ThenBy(c => c.Nombre).ToListAsync();
                var conteos = await _context.Productos
                    .Where(p => p.Estado == TipoEstadoProducto.Activo)
                    .GroupBy(p => p.CategoriaId)
                    .Select(g => new { CategoriaId = g.Key, Total = g.Count() })
                    .ToDictionaryAsync(x => x.CategoriaId, x => x.Total);

                var dtos = categorias.Select(c => c.ToDto(conteos.GetValueOrDefault(c.CategoriaId, 0))).ToList();
                return ResultadoOperacion<List<CategoriaDto>>.SetExito(dtos);
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<List<CategoriaDto>>.SetError("Error al listar categorías: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<List<CategoriaArbolDto>>> ObtenerArbolAsync()
        {
            try
            {
                var todas = await _context.Categorias.AsNoTracking()
                    .Where(c => c.Activa)
                    .OrderBy(c => c.Orden).ThenBy(c => c.Nombre)
                    .ToListAsync();

                var conteos = await _context.Productos
                    .Where(p => p.Estado == TipoEstadoProducto.Activo)
                    .GroupBy(p => p.CategoriaId)
                    .Select(g => new { CategoriaId = g.Key, Total = g.Count() })
                    .ToDictionaryAsync(x => x.CategoriaId, x => x.Total);

                var nodos = todas.ToDictionary(c => c.CategoriaId,
                    c => c.ToArbolDto(conteos.GetValueOrDefault(c.CategoriaId, 0)));

                var raices = new List<CategoriaArbolDto>();
                foreach (var c in todas)
                {
                    var nodo = nodos[c.CategoriaId];
                    if (c.CategoriaPadreId == null)
                        raices.Add(nodo);
                    else if (nodos.TryGetValue(c.CategoriaPadreId.Value, out var padre))
                        padre.Subcategorias.Add(nodo);
                }

                return ResultadoOperacion<List<CategoriaArbolDto>>.SetExito(raices);
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<List<CategoriaArbolDto>>.SetError("Error: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<CategoriaDto>> ObtenerPorIdAsync(int id)
        {
            try
            {
                var c = await _context.Categorias.AsNoTracking().FirstOrDefaultAsync(c => c.CategoriaId == id);
                if (c == null) return ResultadoOperacion<CategoriaDto>.SetError("Categoría no encontrada.");
                var total = await _context.Productos.CountAsync(p =>
                    p.CategoriaId == id && p.Estado == TipoEstadoProducto.Activo);
                return ResultadoOperacion<CategoriaDto>.SetExito(c.ToDto(total));
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<CategoriaDto>.SetError("Error: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<CategoriaDto>> CrearAsync(CrearCategoriaDto dto)
        {
            try
            {
                if (dto == null) return ResultadoOperacion<CategoriaDto>.SetError("El DTO es nulo.");

                var slug = GenerarSlug(dto.Nombre);
                if (await _context.Categorias.AnyAsync(c => c.Slug == slug))
                    return ResultadoOperacion<CategoriaDto>.SetError("Ya existe una categoría con un nombre similar.");

                if (dto.CategoriaPadreId.HasValue &&
                    !await _context.Categorias.AnyAsync(c => c.CategoriaId == dto.CategoriaPadreId))
                    return ResultadoOperacion<CategoriaDto>.SetError("La categoría padre no existe.");

                var categoria = new Categoria
                {
                    CategoriaPadreId = dto.CategoriaPadreId,
                    Nombre = dto.Nombre.Trim(),
                    Slug = slug,
                    Descripcion = dto.Descripcion,
                    ImagenUrl = dto.ImagenUrl,
                    Orden = dto.Orden,
                    Activa = true
                };

                _context.Categorias.Add(categoria);
                await _context.SaveChangesAsync();
                return ResultadoOperacion<CategoriaDto>.SetExito(categoria.ToDto(0));
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<CategoriaDto>.SetError("Error al crear: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<CategoriaDto>> ActualizarAsync(int id, ActualizarCategoriaDto dto)
        {
            try
            {
                var c = await _context.Categorias.FirstOrDefaultAsync(x => x.CategoriaId == id);
                if (c == null) return ResultadoOperacion<CategoriaDto>.SetError("Categoría no encontrada.");

                var nuevoSlug = GenerarSlug(dto.Nombre);
                if (nuevoSlug != c.Slug &&
                    await _context.Categorias.AnyAsync(x => x.Slug == nuevoSlug && x.CategoriaId != id))
                    return ResultadoOperacion<CategoriaDto>.SetError("Ya existe otra categoría con un nombre similar.");

                c.Nombre = dto.Nombre.Trim();
                c.Slug = nuevoSlug;
                c.Descripcion = dto.Descripcion;
                c.ImagenUrl = dto.ImagenUrl;
                c.Orden = dto.Orden;
                c.Activa = dto.Activa;

                await _context.SaveChangesAsync();
                return ResultadoOperacion<CategoriaDto>.SetExito(c.ToDto());
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<CategoriaDto>.SetError("Error al actualizar: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<bool>> DesactivarAsync(int id)
        {
            try
            {
                var c = await _context.Categorias.FirstOrDefaultAsync(x => x.CategoriaId == id);
                if (c == null) return ResultadoOperacion<bool>.SetError("Categoría no encontrada.");

                var tieneProductosActivos = await _context.Productos.AnyAsync(p =>
                    p.CategoriaId == id && p.Estado == TipoEstadoProducto.Activo);
                if (tieneProductosActivos)
                    return ResultadoOperacion<bool>.SetError(
                        "No se puede desactivar: hay productos activos en esta categoría.");

                c.Activa = false;
                await _context.SaveChangesAsync();
                return ResultadoOperacion<bool>.SetExito(true);
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<bool>.SetError("Error: " + ex.Message);
            }
        }

        private static string GenerarSlug(string texto)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var c in texto.Trim().ToLowerInvariant())
            {
                if (char.IsLetterOrDigit(c)) sb.Append(c);
                else if (c == ' ' || c == '-' || c == '_') sb.Append('-');
                else if (c == 'á') sb.Append('a');
                else if (c == 'é') sb.Append('e');
                else if (c == 'í') sb.Append('i');
                else if (c == 'ó') sb.Append('o');
                else if (c == 'ú' || c == 'ü') sb.Append('u');
                else if (c == 'ñ') sb.Append('n');
            }
            return System.Text.RegularExpressions.Regex.Replace(sb.ToString(), "-+", "-").Trim('-');
        }
    }
}
