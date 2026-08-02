using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Modelo.VentaXqm;
using TiendaVirtual.Dominio.Utilidad;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.Sistema;
using TiendaVirtual.Intercambio.Dto.VentaXqm;

namespace TiendaVirtual.Dominio.Servicios.VentaXqm.Implementacion
{
    public class CuponPedidoServicio : ICuponPedidoServicio
    {
        private readonly TiendaVirtualDbContext _context;
        private readonly ILogger<CuponPedidoServicio> _logger;

        public CuponPedidoServicio(TiendaVirtualDbContext context, ILogger<CuponPedidoServicio> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ResultadoOperacion<List<CuponPedidoDto>>> ListarAsync()
        {
            try
            {
                var list = await _context.CuponesPedido.AsNoTracking()
                    .OrderByDescending(c => c.CuponPedidoId)
                    .ToListAsync();
                return ResultadoOperacion<List<CuponPedidoDto>>.SetExito(list.Select(ToDto).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en CuponPedidoServicio.ListarAsync");
                return ResultadoOperacion<List<CuponPedidoDto>>.SetError("Ocurrió un error inesperado.");
            }
        }

        public async Task<ResultadoOperacion<CuponPedidoDto>> CrearAsync(CrearCuponPedidoDto dto)
        {
            try
            {
                if (dto == null || string.IsNullOrWhiteSpace(dto.Codigo))
                    return ResultadoOperacion<CuponPedidoDto>.SetError("El código es obligatorio.");

                if (dto.TipoDescuento == null)
                    return ResultadoOperacion<CuponPedidoDto>.SetError("El tipo de descuento es obligatorio.");

                var codigo = dto.Codigo.Trim().ToUpperInvariant();
                if (await _context.CuponesPedido.AnyAsync(c => c.Codigo == codigo))
                    return ResultadoOperacion<CuponPedidoDto>.SetError("Ya existe un cupón con ese código.");

                var tipo = (TipoDescuentoCupon)dto.TipoDescuento.Id;
                var errorTipo = ValidarTipoYValor(tipo, dto.ValorDescuento);
                if (errorTipo != null)
                    return ResultadoOperacion<CuponPedidoDto>.SetError(errorTipo);

                if (dto.MontoMinimo is < 0)
                    return ResultadoOperacion<CuponPedidoDto>.SetError("El monto mínimo no puede ser negativo.");

                var cupon = new CuponPedido
                {
                    Codigo = codigo,
                    TipoDescuento = tipo,
                    ValorDescuento = Math.Round(dto.ValorDescuento, 2),
                    MontoMinimo = dto.MontoMinimo.HasValue ? Math.Round(dto.MontoMinimo.Value, 2) : null,
                    UsosMaximos = NormalizarUsosMaximos(dto.UsosMaximos),
                    UsosRealizados = 0,
                    ValidoHasta = NormalizarValidoHasta(dto.ValidoHasta),
                    Activo = true,
                    Descripcion = string.IsNullOrWhiteSpace(dto.Descripcion) ? null : dto.Descripcion.Trim(),
                    FechaCreacion = DateTime.UtcNow
                };

                _context.CuponesPedido.Add(cupon);
                await _context.SaveChangesAsync();
                return ResultadoOperacion<CuponPedidoDto>.SetExito(ToDto(cupon));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en CuponPedidoServicio.CrearAsync");
                return ResultadoOperacion<CuponPedidoDto>.SetError("Ocurrió un error inesperado.");
            }
        }

        public async Task<ResultadoOperacion<CuponPedidoDto>> ActualizarAsync(int id, ActualizarCuponPedidoDto dto)
        {
            try
            {
                if (dto == null)
                    return ResultadoOperacion<CuponPedidoDto>.SetError("Datos requeridos.");

                var cupon = await _context.CuponesPedido.FirstOrDefaultAsync(c => c.CuponPedidoId == id);
                if (cupon == null)
                    return ResultadoOperacion<CuponPedidoDto>.SetError("Cupón no encontrado.");

                var errorTipo = ValidarTipoYValor(cupon.TipoDescuento, dto.ValorDescuento);
                if (errorTipo != null)
                    return ResultadoOperacion<CuponPedidoDto>.SetError(errorTipo);

                if (dto.MontoMinimo is < 0)
                    return ResultadoOperacion<CuponPedidoDto>.SetError("El monto mínimo no puede ser negativo.");

                var usosMax = NormalizarUsosMaximos(dto.UsosMaximos);
                if (usosMax.HasValue && usosMax.Value < cupon.UsosRealizados)
                    return ResultadoOperacion<CuponPedidoDto>.SetError(
                        $"Los usos máximos no pueden ser menores a los ya realizados ({cupon.UsosRealizados}).");

                cupon.ValorDescuento = Math.Round(dto.ValorDescuento, 2);
                cupon.MontoMinimo = dto.MontoMinimo.HasValue ? Math.Round(dto.MontoMinimo.Value, 2) : null;
                cupon.UsosMaximos = usosMax;
                cupon.ValidoHasta = NormalizarValidoHasta(dto.ValidoHasta);
                cupon.Activo = dto.Activo;
                cupon.Descripcion = string.IsNullOrWhiteSpace(dto.Descripcion) ? null : dto.Descripcion.Trim();

                await _context.SaveChangesAsync();
                return ResultadoOperacion<CuponPedidoDto>.SetExito(ToDto(cupon));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en CuponPedidoServicio.ActualizarAsync");
                return ResultadoOperacion<CuponPedidoDto>.SetError("Ocurrió un error inesperado.");
            }
        }

        public async Task<ResultadoOperacion<bool>> ActivarAsync(int id)
        {
            try
            {
                var cupon = await _context.CuponesPedido.FirstOrDefaultAsync(c => c.CuponPedidoId == id);
                if (cupon == null)
                    return ResultadoOperacion<bool>.SetError("Cupón no encontrado.");
                cupon.Activo = true;
                await _context.SaveChangesAsync();
                return ResultadoOperacion<bool>.SetExito(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en CuponPedidoServicio.ActivarAsync");
                return ResultadoOperacion<bool>.SetError("Ocurrió un error inesperado.");
            }
        }

        public async Task<ResultadoOperacion<bool>> DesactivarAsync(int id)
        {
            try
            {
                var cupon = await _context.CuponesPedido.FirstOrDefaultAsync(c => c.CuponPedidoId == id);
                if (cupon == null)
                    return ResultadoOperacion<bool>.SetError("Cupón no encontrado.");
                cupon.Activo = false;
                await _context.SaveChangesAsync();
                return ResultadoOperacion<bool>.SetExito(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en CuponPedidoServicio.DesactivarAsync");
                return ResultadoOperacion<bool>.SetError("Ocurrió un error inesperado.");
            }
        }

        private static string? ValidarTipoYValor(TipoDescuentoCupon tipo, decimal valor) =>
            tipo switch
            {
                TipoDescuentoCupon.Porcentaje when valor is <= 0 or > 100 =>
                    "El porcentaje debe ser entre 1 y 100.",
                TipoDescuentoCupon.MontoFijo when valor <= 0 =>
                    "El monto fijo debe ser mayor a 0.",
                TipoDescuentoCupon.MesesGratis =>
                    "Los cupones de pedido solo admiten porcentaje o monto fijo.",
                _ when tipo is not (TipoDescuentoCupon.Porcentaje or TipoDescuentoCupon.MontoFijo) =>
                    "Tipo de descuento inválido.",
                _ => null
            };

        private static int? NormalizarUsosMaximos(int? usos) =>
            usos is null or <= 0 ? null : usos;

        private static DateTime? NormalizarValidoHasta(DateTime? fecha) =>
            fecha.HasValue ? FechaHoraUtil.AUtc(fecha.Value) : null;

        private static CuponPedidoDto ToDto(CuponPedido c) => new()
        {
            CuponPedidoId = c.CuponPedidoId,
            Codigo = c.Codigo,
            TipoDescuento = new EnumeracionDto
            {
                Id = (int)c.TipoDescuento,
                Nombre = c.TipoDescuento.GetDescription()
            },
            ValorDescuento = c.ValorDescuento,
            MontoMinimo = c.MontoMinimo,
            UsosMaximos = c.UsosMaximos,
            UsosRealizados = c.UsosRealizados,
            ValidoHasta = c.ValidoHasta,
            Activo = c.Activo,
            Descripcion = c.Descripcion,
            FechaCreacion = c.FechaCreacion
        };
    }
}
