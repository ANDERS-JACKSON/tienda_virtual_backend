using Microsoft.EntityFrameworkCore;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Extensiones.VendedorXqm;
using TiendaVirtual.Dominio.Modelo.SoporteXqm;
using TiendaVirtual.Dominio.Modelo.VendedorXqm;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.Sistema;
using TiendaVirtual.Intercambio.Dto.VendedorXqm;

namespace TiendaVirtual.Dominio.Servicios.SuscripcionXqm.Implementacion
{
    public class SuscripcionServicio : ISuscripcionServicio
    {
        private readonly TiendaVirtualDbContext _context;

        public SuscripcionServicio(TiendaVirtualDbContext context) => _context = context;

        public async Task<ResultadoOperacion<SuscripcionDto?>> ObtenerMiSuscripcionAsync(int usuarioId)
        {
            try
            {
                var vendedor = await _context.Vendedores.AsNoTracking()
                    .FirstOrDefaultAsync(v => v.UsuarioId == usuarioId);
                if (vendedor == null)
                    return ResultadoOperacion<SuscripcionDto?>.SetError("No tienes perfil de vendedor.");

                var now = DateTime.UtcNow;
                var sus = await _context.Suscripciones.AsNoTracking()
                    .Include(s => s.Plan)
                    .Include(s => s.Cupon)
                    .Where(s => s.VendedorId == vendedor.VendedorId)
                    .OrderByDescending(s => s.SuscripcionId)
                    .FirstOrDefaultAsync(s =>
                        s.Estado == TipoEstadoSuscripcion.EnPrueba ||
                        s.Estado == TipoEstadoSuscripcion.Activa ||
                        s.Estado == TipoEstadoSuscripcion.PendientePago ||
                        (s.Estado == TipoEstadoSuscripcion.Cancelada &&
                         s.PeriodoFin.HasValue && s.PeriodoFin > now));

                return ResultadoOperacion<SuscripcionDto?>.SetExito(sus?.ToDto());
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<SuscripcionDto?>.SetError("Error: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<SuscripcionElegibilidadDto>> ObtenerElegibilidadAsync(int usuarioId)
        {
            try
            {
                var vendedor = await _context.Vendedores.AsNoTracking()
                    .FirstOrDefaultAsync(v => v.UsuarioId == usuarioId);
                if (vendedor == null)
                    return ResultadoOperacion<SuscripcionElegibilidadDto>.SetError("No tienes perfil de vendedor.");

                var now = DateTime.UtcNow;
                var elegibleMesesGratis = await SuscripcionBeneficiosHelper.ElegibleMesesGratisAsync(
                    _context, vendedor.VendedorId);
                var bloqueante = await TieneSuscripcionBloqueanteAsync(vendedor.VendedorId, now);
                var tieneCanceladaConAcceso = await _context.Suscripciones.AnyAsync(s =>
                    s.VendedorId == vendedor.VendedorId &&
                    s.Estado == TipoEstadoSuscripcion.Cancelada &&
                    s.PeriodoFin.HasValue && s.PeriodoFin > now);

                var puedeContratar = !bloqueante || tieneCanceladaConAcceso;
                var requierePago = !elegibleMesesGratis;

                string mensaje;
                if (tieneCanceladaConAcceso)
                    mensaje =
                        "Tu suscripción está cancelada. Tu tienda y productos ya no son visibles para compradores. " +
                        "Reactiva tu plan y completa el pago para volver a vender.";
                else if (requierePago)
                    mensaje =
                        "Al contratar de nuevo no hay meses gratis: deberás pagar el plan para activar la suscripción.";
                else
                    mensaje =
                        $"Tu primera suscripción incluye {SuscripcionBeneficiosHelper.MesesPruebaPrimeraVez} meses de prueba gratis.";

                return ResultadoOperacion<SuscripcionElegibilidadDto>.SetExito(new SuscripcionElegibilidadDto
                {
                    ElegibleMesesGratis = elegibleMesesGratis,
                    PuedeContratarNueva = puedeContratar,
                    RequierePagoInmediato = requierePago,
                    TieneCanceladaConAcceso = tieneCanceladaConAcceso,
                    Mensaje = mensaje
                });
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<SuscripcionElegibilidadDto>.SetError("Error: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<SuscripcionDto>> IniciarAsync(int usuarioId, IniciarSuscripcionDto dto)
        {
            await using var trx = await _context.Database.BeginTransactionAsync();
            try
            {
                var vendedor = await _context.Vendedores.FirstOrDefaultAsync(v => v.UsuarioId == usuarioId);
                if (vendedor == null)
                    return ResultadoOperacion<SuscripcionDto>.SetError("No tienes perfil de vendedor.");

                if (vendedor.Estado != TipoEstadoVendedor.Activo)
                    return ResultadoOperacion<SuscripcionDto>.SetError(
                        "Tu cuenta debe estar verificada antes de iniciar una suscripción.");

                var now = DateTime.UtcNow;
                if (await TieneSuscripcionBloqueanteAsync(vendedor.VendedorId, now))
                    return ResultadoOperacion<SuscripcionDto>.SetError(
                        "Ya tienes una suscripción en curso. Cancela primero o usa 'Reactivar plan' si ya cancelaste.");

                return await CrearSuscripcionAsync(usuarioId, vendedor, dto, forzarPago: false, trx);
            }
            catch (Exception ex)
            {
                await trx.RollbackAsync();
                return ResultadoOperacion<SuscripcionDto>.SetError("Error al iniciar suscripción: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<SuscripcionDto>> ReactivarPlanAsync(int usuarioId, IniciarSuscripcionDto dto)
        {
            await using var trx = await _context.Database.BeginTransactionAsync();
            try
            {
                var vendedor = await _context.Vendedores.FirstOrDefaultAsync(v => v.UsuarioId == usuarioId);
                if (vendedor == null)
                    return ResultadoOperacion<SuscripcionDto>.SetError("No tienes perfil de vendedor.");

                if (vendedor.Estado != TipoEstadoVendedor.Activo)
                    return ResultadoOperacion<SuscripcionDto>.SetError(
                        "Tu cuenta debe estar verificada antes de reactivar una suscripción.");

                var now = DateTime.UtcNow;
                await FinalizarSuscripcionesCanceladasParaReactivarAsync(vendedor.VendedorId, now);

                if (await TieneSuscripcionBloqueanteAsync(vendedor.VendedorId, now))
                    return ResultadoOperacion<SuscripcionDto>.SetError(
                        "Aún tienes una suscripción activa. Cancélala antes de reactivar con otro plan.");

                return await CrearSuscripcionAsync(usuarioId, vendedor, dto, forzarPago: true, trx);
            }
            catch (Exception ex)
            {
                await trx.RollbackAsync();
                return ResultadoOperacion<SuscripcionDto>.SetError("Error al reactivar suscripción: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<SuscripcionDto>> CambiarPlanAsync(int usuarioId, CambiarPlanDto dto)
        {
            await using var trx = await _context.Database.BeginTransactionAsync();
            try
            {
                var vendedor = await _context.Vendedores.FirstOrDefaultAsync(v => v.UsuarioId == usuarioId);
                if (vendedor == null)
                    return ResultadoOperacion<SuscripcionDto>.SetError("No tienes perfil de vendedor.");

                var sus = await ObtenerSuscripcionGestionableAsync(vendedor.VendedorId);
                if (sus == null)
                    return ResultadoOperacion<SuscripcionDto>.SetError("No tienes una suscripción activa.");

                if (sus.PlanId == dto.NuevoPlanId)
                    return ResultadoOperacion<SuscripcionDto>.SetError("Ya estás en ese plan.");

                var nuevoPlan = await _context.Planes.FirstOrDefaultAsync(p => p.PlanId == dto.NuevoPlanId && p.Activo);
                if (nuevoPlan == null)
                    return ResultadoOperacion<SuscripcionDto>.SetError("Plan no encontrado o inactivo.");

                sus.PlanId = nuevoPlan.PlanId;
                await _context.SaveChangesAsync();

                _context.Notificaciones.Add(new Notificacion
                {
                    UsuarioId = usuarioId,
                    Tipo = "SUSCRIPCION_PLAN_CAMBIADO",
                    Titulo = "Cambio de plan agendado",
                    Cuerpo = $"Tu suscripción cambiará a {nuevoPlan.Nombre} al inicio del próximo periodo.",
                    Leida = false,
                    Fecha = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
                await trx.CommitAsync();

                return await CargarSuscripcionAsync(sus.SuscripcionId);
            }
            catch (Exception ex)
            {
                await trx.RollbackAsync();
                return ResultadoOperacion<SuscripcionDto>.SetError("Error: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<bool>> CancelarAsync(int usuarioId)
        {
            try
            {
                var vendedor = await _context.Vendedores.FirstOrDefaultAsync(v => v.UsuarioId == usuarioId);
                if (vendedor == null)
                    return ResultadoOperacion<bool>.SetError("No tienes perfil de vendedor.");

                var sus = await ObtenerSuscripcionGestionableAsync(vendedor.VendedorId);
                if (sus == null)
                    return ResultadoOperacion<bool>.SetError("No hay suscripción que cancelar.");

                sus.Estado = TipoEstadoSuscripcion.Cancelada;
                await _context.SaveChangesAsync();

                var finAcceso = sus.PeriodoFin.HasValue
                    ? sus.PeriodoFin.Value.ToString("dd/MM/yyyy")
                    : "el fin de tu periodo";
                _context.Notificaciones.Add(new Notificacion
                {
                    UsuarioId = usuarioId,
                    Tipo = "SUSCRIPCION_CANCELADA",
                    Titulo = "Suscripción cancelada",
                    Cuerpo =
                        $"Tu tienda y productos dejarán de mostrarse al público. " +
                        "Para volver a vender debes reactivar el plan y pagar (sin meses gratis).",
                    Leida = false,
                    Fecha = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();

                return ResultadoOperacion<bool>.SetExito(true);
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<bool>.SetError("Error: " + ex.Message);
            }
        }

        public async Task<bool> PuedeVendedorPublicarAsync(int vendedorId)
        {
            var now = DateTime.UtcNow;
            var activas = await _context.Suscripciones.AsNoTracking()
                .Where(s => s.VendedorId == vendedorId)
                .ToListAsync();
            return activas.Any(s => SuscripcionBeneficiosHelper.EsComercialmenteActiva(s, now));
        }

        public async Task<ResultadoOperacion<PaginacionRespuestaDto<SuscripcionDto>>> ListarAdminAsync(
            int pagina, int tamanioPagina)
        {
            try
            {
                pagina = Math.Max(1, pagina);
                tamanioPagina = Math.Clamp(tamanioPagina, 1, 50);

                var query = _context.Suscripciones.AsNoTracking()
                    .Include(s => s.Plan)
                    .Include(s => s.Cupon)
                    .Include(s => s.Vendedor)
                    .ThenInclude(v => v.Usuario)
                    .OrderByDescending(s => s.SuscripcionId);

                var total = await query.CountAsync();
                var items = await query.Skip((pagina - 1) * tamanioPagina).Take(tamanioPagina).ToListAsync();

                return ResultadoOperacion<PaginacionRespuestaDto<SuscripcionDto>>.SetExito(
                    new PaginacionRespuestaDto<SuscripcionDto>
                    {
                        Items = items.Select(s => s.ToDto(
                            s.Vendedor.NombreTienda,
                            s.Vendedor.Usuario.Correo)).ToList(),
                        Pagina = pagina,
                        TamanioPagina = tamanioPagina,
                        TotalRegistros = total,
                        HayMas = pagina * tamanioPagina < total
                    });
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<PaginacionRespuestaDto<SuscripcionDto>>.SetError("Error: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<bool>> SuspenderAsync(int suscripcionId)
        {
            var sus = await _context.Suscripciones.FirstOrDefaultAsync(x => x.SuscripcionId == suscripcionId);
            if (sus == null)
                return ResultadoOperacion<bool>.SetError("Suscripción no encontrada.");
            sus.Estado = TipoEstadoSuscripcion.Suspendida;
            await _context.SaveChangesAsync();
            return ResultadoOperacion<bool>.SetExito(true);
        }

        public async Task<ResultadoOperacion<bool>> ReactivarAsync(int suscripcionId)
        {
            var sus = await _context.Suscripciones.FirstOrDefaultAsync(x => x.SuscripcionId == suscripcionId);
            if (sus == null)
                return ResultadoOperacion<bool>.SetError("Suscripción no encontrada.");
            if (sus.Estado != TipoEstadoSuscripcion.Suspendida)
                return ResultadoOperacion<bool>.SetError("Solo se pueden reactivar suscripciones suspendidas.");
            sus.Estado = TipoEstadoSuscripcion.PendientePago;
            await _context.SaveChangesAsync();
            return ResultadoOperacion<bool>.SetExito(true);
        }

        public async Task<ResultadoOperacion<int>> ProcesarVencimientosAsync()
        {
            try
            {
                var now = DateTime.UtcNow;
                var afectadas = 0;

                var trialsVencidos = await _context.Suscripciones
                    .Where(s => s.Estado == TipoEstadoSuscripcion.EnPrueba &&
                                s.PruebaTerminaEn.HasValue && s.PruebaTerminaEn <= now)
                    .ToListAsync();
                foreach (var s in trialsVencidos)
                    s.Estado = TipoEstadoSuscripcion.PendientePago;
                afectadas += trialsVencidos.Count;

                var periodosVencidos = await _context.Suscripciones
                    .Where(s => s.Estado == TipoEstadoSuscripcion.Activa &&
                                s.PeriodoFin.HasValue && s.PeriodoFin <= now)
                    .ToListAsync();
                foreach (var s in periodosVencidos)
                    s.Estado = TipoEstadoSuscripcion.PendientePago;
                afectadas += periodosVencidos.Count;

                var hace30 = now.AddDays(-30);
                var pendientesVencidas = await _context.Suscripciones
                    .Where(s => s.Estado == TipoEstadoSuscripcion.PendientePago &&
                                ((s.PeriodoFin.HasValue && s.PeriodoFin <= hace30) ||
                                 (s.PruebaTerminaEn.HasValue && s.PruebaTerminaEn <= hace30)))
                    .ToListAsync();
                foreach (var s in pendientesVencidas)
                    s.Estado = TipoEstadoSuscripcion.Vencida;
                afectadas += pendientesVencidas.Count;

                var canceladasVencidas = await _context.Suscripciones
                    .Where(s => s.Estado == TipoEstadoSuscripcion.Cancelada &&
                                s.PeriodoFin.HasValue && s.PeriodoFin <= now)
                    .ToListAsync();
                foreach (var s in canceladasVencidas)
                    s.Estado = TipoEstadoSuscripcion.Vencida;
                afectadas += canceladasVencidas.Count;

                await _context.SaveChangesAsync();
                return ResultadoOperacion<int>.SetExito(afectadas);
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<int>.SetError("Error: " + ex.Message);
            }
        }

        private async Task<bool> TieneSuscripcionBloqueanteAsync(int vendedorId, DateTime now) =>
            await _context.Suscripciones.AnyAsync(s =>
                s.VendedorId == vendedorId &&
                (s.Estado == TipoEstadoSuscripcion.EnPrueba ||
                 s.Estado == TipoEstadoSuscripcion.Activa ||
                 s.Estado == TipoEstadoSuscripcion.PendientePago ||
                 (s.Estado == TipoEstadoSuscripcion.Cancelada &&
                  s.PeriodoFin.HasValue && s.PeriodoFin > now)));

        private async Task<Suscripcion?> ObtenerSuscripcionGestionableAsync(int vendedorId) =>
            await _context.Suscripciones.Include(s => s.Plan)
                .OrderByDescending(s => s.SuscripcionId)
                .FirstOrDefaultAsync(s => s.VendedorId == vendedorId &&
                    (s.Estado == TipoEstadoSuscripcion.EnPrueba ||
                     s.Estado == TipoEstadoSuscripcion.Activa ||
                     s.Estado == TipoEstadoSuscripcion.PendientePago));

        private async Task<ResultadoOperacion<SuscripcionDto>> CargarSuscripcionAsync(int suscripcionId)
        {
            var s = await _context.Suscripciones.AsNoTracking()
                .Include(x => x.Plan)
                .Include(x => x.Cupon)
                .FirstAsync(x => x.SuscripcionId == suscripcionId);
            return ResultadoOperacion<SuscripcionDto>.SetExito(s.ToDto());
        }

        private async Task FinalizarSuscripcionesCanceladasParaReactivarAsync(int vendedorId, DateTime now)
        {
            var canceladasConAcceso = await _context.Suscripciones
                .Where(s => s.VendedorId == vendedorId &&
                            s.Estado == TipoEstadoSuscripcion.Cancelada &&
                            s.PeriodoFin.HasValue && s.PeriodoFin > now)
                .ToListAsync();

            foreach (var s in canceladasConAcceso)
            {
                s.PeriodoFin = now;
                s.Estado = TipoEstadoSuscripcion.Vencida;
            }

            if (canceladasConAcceso.Count > 0)
                await _context.SaveChangesAsync();
        }

        private async Task<ResultadoOperacion<SuscripcionDto>> CrearSuscripcionAsync(
            int usuarioId,
            Vendedor vendedor,
            IniciarSuscripcionDto dto,
            bool forzarPago,
            Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction trx)
        {
            var now = DateTime.UtcNow;
            var plan = await _context.Planes.FirstOrDefaultAsync(p => p.PlanId == dto.PlanId && p.Activo);
            if (plan == null)
                return ResultadoOperacion<SuscripcionDto>.SetError("Plan no encontrado o inactivo.");

            var elegibleMesesGratis = forzarPago
                ? false
                : await SuscripcionBeneficiosHelper.ElegibleMesesGratisAsync(_context, vendedor.VendedorId);

            Cupon? cupon = null;
            if (!string.IsNullOrWhiteSpace(dto.CodigoCupon))
            {
                var codigo = dto.CodigoCupon.Trim().ToUpperInvariant();
                cupon = await _context.Cupones.FirstOrDefaultAsync(c => c.Codigo == codigo);

                if (cupon == null || !cupon.Activo)
                    return ResultadoOperacion<SuscripcionDto>.SetError("Cupón no válido.");
                if (cupon.ValidoHasta.HasValue && cupon.ValidoHasta < now)
                    return ResultadoOperacion<SuscripcionDto>.SetError("Cupón vencido.");
                if (cupon.UsosMaximos.HasValue && cupon.UsosRealizados >= cupon.UsosMaximos)
                    return ResultadoOperacion<SuscripcionDto>.SetError("Cupón agotado.");

                var yaUsado = await _context.Suscripciones.AnyAsync(s =>
                    s.VendedorId == vendedor.VendedorId && s.CuponId == cupon.CuponId);
                if (yaUsado)
                    return ResultadoOperacion<SuscripcionDto>.SetError("Ya usaste este cupón antes.");
            }

            var (mesesPruebaBase, mesesExtraCupon, errorMesesGratis) =
                SuscripcionBeneficiosHelper.CalcularMesesGratis(elegibleMesesGratis, cupon);
            if (errorMesesGratis != null)
                return ResultadoOperacion<SuscripcionDto>.SetError(errorMesesGratis);

            if (cupon != null)
                cupon.UsosRealizados++;

            var mesesGratisTotal = mesesPruebaBase + mesesExtraCupon;
            DateTime? pruebaTerminaEn = mesesGratisTotal > 0 ? now.AddMonths(mesesGratisTotal) : null;

            var estado = mesesGratisTotal > 0
                ? TipoEstadoSuscripcion.EnPrueba
                : TipoEstadoSuscripcion.PendientePago;

            var suscripcion = new Suscripcion
            {
                VendedorId = vendedor.VendedorId,
                PlanId = plan.PlanId,
                Estado = estado,
                MesesGratisOtorgados = (short)mesesGratisTotal,
                CuponId = cupon?.CuponId,
                PruebaTerminaEn = pruebaTerminaEn,
                PeriodoInicio = estado == TipoEstadoSuscripcion.EnPrueba ? now : null,
                PeriodoFin = estado == TipoEstadoSuscripcion.EnPrueba ? pruebaTerminaEn : null
            };
            _context.Suscripciones.Add(suscripcion);
            await _context.SaveChangesAsync();

            var tipoNotif = forzarPago ? "SUSCRIPCION_REACTIVADA" : "SUSCRIPCION_INICIADA";
            var titulo = forzarPago
                ? $"Plan {plan.Nombre} — reactivación"
                : $"¡Suscripción al plan {plan.Nombre} iniciada!";
            var cuerpo = estado == TipoEstadoSuscripcion.EnPrueba
                ? $"Tienes {mesesGratisTotal} mes(es) gratis hasta {pruebaTerminaEn:dd/MM/yyyy}."
                : "Tu suscripción está pendiente de pago. Completa el pago para volver a publicar productos.";

            _context.Notificaciones.Add(new Notificacion
            {
                UsuarioId = usuarioId,
                Tipo = tipoNotif,
                Titulo = titulo,
                Cuerpo = cuerpo,
                Leida = false,
                Fecha = now
            });
            await _context.SaveChangesAsync();
            await trx.CommitAsync();

            return await CargarSuscripcionAsync(suscripcion.SuscripcionId);
        }
    }
}
