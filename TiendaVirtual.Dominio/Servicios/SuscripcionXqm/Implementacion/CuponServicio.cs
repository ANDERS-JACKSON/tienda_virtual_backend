using Microsoft.EntityFrameworkCore;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Extensiones.VendedorXqm;
using TiendaVirtual.Dominio.Modelo.VendedorXqm;
using TiendaVirtual.Dominio.Utilidad;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.VendedorXqm;

namespace TiendaVirtual.Dominio.Servicios.SuscripcionXqm.Implementacion
{
    public class CuponServicio : ICuponServicio
    {
        private readonly TiendaVirtualDbContext _context;

        public CuponServicio(TiendaVirtualDbContext context) => _context = context;

        public async Task<ResultadoOperacion<List<CuponDto>>> ListarAsync()
        {
            try
            {
                var cupones = await _context.Cupones.AsNoTracking()
                    .OrderByDescending(c => c.CuponId)
                    .ToListAsync();
                return ResultadoOperacion<List<CuponDto>>.SetExito(cupones.Select(c => c.ToDto()).ToList());
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<List<CuponDto>>.SetError("Error: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<CuponDto>> CrearAsync(CrearCuponDto dto)
        {
            try
            {
                var codigo = dto.Codigo.Trim().ToUpperInvariant();
                if (await _context.Cupones.AnyAsync(c => c.Codigo == codigo))
                    return ResultadoOperacion<CuponDto>.SetError("Ya existe un cupón con ese código.");

                var tipo = (TipoDescuentoCupon)dto.TipoDescuento.Id;
                var errorTipo = ValidarCoherenciaTipo(tipo, dto.ValorDescuento, dto.MesesGratis);
                if (errorTipo != null)
                    return ResultadoOperacion<CuponDto>.SetError(errorTipo);

                var cupon = new Cupon
                {
                    Codigo = codigo,
                    TipoDescuento = tipo,
                    ValorDescuento = dto.ValorDescuento,
                    MesesGratis = (short)dto.MesesGratis,
                    UsosMaximos = NormalizarUsosMaximos(dto.UsosMaximos),
                    UsosRealizados = 0,
                    ValidoHasta = NormalizarValidoHasta(dto.ValidoHasta),
                    Activo = true
                };
                _context.Cupones.Add(cupon);
                await _context.SaveChangesAsync();
                return ResultadoOperacion<CuponDto>.SetExito(cupon.ToDto());
            }
            catch (DbUpdateException ex)
            {
                return ResultadoOperacion<CuponDto>.SetError(ObtenerMensajeDb(ex));
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<CuponDto>.SetError("Error: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<CuponDto>> ActualizarAsync(int id, ActualizarCuponDto dto)
        {
            try
            {
                if (dto == null)
                    return ResultadoOperacion<CuponDto>.SetError("Datos requeridos.");

                var cupon = await _context.Cupones.FirstOrDefaultAsync(x => x.CuponId == id);
                if (cupon == null)
                    return ResultadoOperacion<CuponDto>.SetError("Cupón no encontrado.");

                var errorTipo = ValidarCoherenciaTipo(cupon.TipoDescuento, dto.ValorDescuento, dto.MesesGratis);
                if (errorTipo != null)
                    return ResultadoOperacion<CuponDto>.SetError(errorTipo);

                var usosMaximos = NormalizarUsosMaximos(dto.UsosMaximos);
                if (usosMaximos.HasValue && usosMaximos.Value < cupon.UsosRealizados)
                    return ResultadoOperacion<CuponDto>.SetError(
                        $"Los usos máximos no pueden ser menores a los usos ya realizados ({cupon.UsosRealizados}).");

                cupon.ValorDescuento = dto.ValorDescuento;
                cupon.MesesGratis = (short)dto.MesesGratis;
                cupon.UsosMaximos = usosMaximos;
                cupon.ValidoHasta = NormalizarValidoHasta(dto.ValidoHasta);
                cupon.Activo = dto.Activo;

                await _context.SaveChangesAsync();
                return ResultadoOperacion<CuponDto>.SetExito(cupon.ToDto());
            }
            catch (DbUpdateException ex)
            {
                return ResultadoOperacion<CuponDto>.SetError(ObtenerMensajeDb(ex));
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<CuponDto>.SetError("Error: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<bool>> DesactivarAsync(int id)
        {
            var cupon = await _context.Cupones.FirstOrDefaultAsync(x => x.CuponId == id);
            if (cupon == null)
                return ResultadoOperacion<bool>.SetError("Cupón no encontrado.");
            cupon.Activo = false;
            await _context.SaveChangesAsync();
            return ResultadoOperacion<bool>.SetExito(true);
        }

        public async Task<ResultadoOperacion<CuponValidadoDto>> ValidarAsync(int usuarioId, ValidarCuponDto dto)
        {
            try
            {
                var codigo = dto.Codigo.Trim().ToUpperInvariant();
                var cupon = await _context.Cupones.AsNoTracking().FirstOrDefaultAsync(c => c.Codigo == codigo);

                if (cupon == null)
                    return ExitoValidacion(false, "Cupón no encontrado.");

                if (!cupon.Activo)
                    return ExitoValidacion(false, "Cupón inactivo.");

                if (cupon.ValidoHasta.HasValue && cupon.ValidoHasta.Value < DateTime.UtcNow)
                    return ExitoValidacion(false, "Cupón vencido.");

                if (cupon.UsosMaximos.HasValue && cupon.UsosRealizados >= cupon.UsosMaximos)
                    return ExitoValidacion(false, "Cupón agotado.");

                var vendedor = await _context.Vendedores.AsNoTracking()
                    .FirstOrDefaultAsync(v => v.UsuarioId == usuarioId);
                if (vendedor == null)
                    return ExitoValidacion(false, "No tienes perfil de vendedor.");

                var yaUsado = await _context.Suscripciones.AnyAsync(s =>
                    s.VendedorId == vendedor.VendedorId && s.CuponId == cupon.CuponId);
                if (yaUsado)
                    return ExitoValidacion(false, "Ya usaste este cupón antes.");

                var elegibleMesesGratis = await SuscripcionBeneficiosHelper.ElegibleMesesGratisAsync(
                    _context, vendedor.VendedorId);
                var (mesesPruebaBase, mesesExtra, errorMeses) =
                    SuscripcionBeneficiosHelper.CalcularMesesGratis(elegibleMesesGratis, cupon);
                if (errorMeses != null)
                    return ExitoValidacion(false, errorMeses);

                var mesesGratisTotal = mesesPruebaBase + mesesExtra;

                decimal? precioOriginal = null;
                decimal? precioConDescuento = null;

                if (dto.PlanId.HasValue)
                {
                    var plan = await _context.Planes.AsNoTracking()
                        .FirstOrDefaultAsync(p => p.PlanId == dto.PlanId);
                    if (plan != null)
                    {
                        precioOriginal = plan.Precio;
                        precioConDescuento = CalcularPrecioConDescuento(plan.Precio, cupon);
                    }
                }

                return ResultadoOperacion<CuponValidadoDto>.SetExito(new CuponValidadoDto
                {
                    Valido = true,
                    Mensaje = "Cupón válido.",
                    Cupon = cupon.ToDto(),
                    PrecioOriginal = precioOriginal,
                    PrecioConDescuento = precioConDescuento,
                    MesesGratisTotal = mesesGratisTotal
                });
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<CuponValidadoDto>.SetError("Error: " + ex.Message);
            }
        }

        private static ResultadoOperacion<CuponValidadoDto> ExitoValidacion(bool valido, string mensaje) =>
            ResultadoOperacion<CuponValidadoDto>.SetExito(new CuponValidadoDto
            {
                Valido = valido,
                Mensaje = mensaje
            });

        private static string? ValidarCoherenciaTipo(TipoDescuentoCupon tipo, decimal? valor, int mesesGratis) =>
            tipo switch
            {
                TipoDescuentoCupon.Porcentaje when valor is null or <= 0 or > 100 =>
                    "El porcentaje debe ser entre 1 y 100.",
                TipoDescuentoCupon.MontoFijo when valor is null or <= 0 =>
                    "El monto fijo debe ser mayor a 0.",
                TipoDescuentoCupon.MesesGratis when mesesGratis <= 0 =>
                    "Los meses gratis deben ser mayor a 0.",
                _ => null
            };

        internal static decimal CalcularPrecioConDescuento(decimal precio, Cupon cupon) =>
            cupon.TipoDescuento switch
            {
                TipoDescuentoCupon.Porcentaje => Math.Round(precio * (1 - (cupon.ValorDescuento ?? 0) / 100), 2),
                TipoDescuentoCupon.MontoFijo => Math.Max(0, precio - (cupon.ValorDescuento ?? 0)),
                _ => precio
            };

        private static DateTime? NormalizarValidoHasta(DateTime? fecha) =>
            fecha.HasValue ? FechaHoraUtil.AUtc(fecha.Value) : null;

        private static int? NormalizarUsosMaximos(int? usosMaximos) =>
            usosMaximos is null or <= 0 ? null : usosMaximos;

        private static string ObtenerMensajeDb(DbUpdateException ex) =>
            ex.InnerException?.Message ?? ex.Message;
    }
}
