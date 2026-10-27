using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Extensiones.VendedorXqm;
using TiendaVirtual.Dominio.Modelo.VendedorXqm;
using TiendaVirtual.Dominio.Servicios.SoporteXqm;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.Sistema;
using TiendaVirtual.Intercambio.Dto.VendedorXqm;

namespace TiendaVirtual.Dominio.Servicios.SuscripcionXqm.Implementacion
{
    public class SuscripcionServicio : ISuscripcionServicio
    {
        private readonly TiendaVirtualDbContext _context;
        private readonly ILogger<SuscripcionServicio> _logger;
        private readonly INotificacionServicio _notificacionServicio;

        public SuscripcionServicio(TiendaVirtualDbContext context, INotificacionServicio notificacionServicio, ILogger<SuscripcionServicio> logger)
        {
            _logger = logger;
            _context = context;
            _notificacionServicio = notificacionServicio;
        }

        public async Task<ResultadoOperacion<SuscripcionDto?>> ObtenerMiSuscripcionAsync(Guid usuarioId)
        {
            try
            {
                var vendedor = await _context.Vendedores.AsNoTracking()
                    .FirstOrDefaultAsync(v => v.UsuarioId == usuarioId);
                if (vendedor == null)
                    return ResultadoOperacion<SuscripcionDto?>.SetError("No tienes perfil de vendedor.");

                var now = DateTime.UtcNow;
                // Tracked: si la prueba/periodo ya venció, pasamos a PendientePago al consultar
                // (no depende solo del job admin de vencimientos).
                var sus = await _context.Suscripciones
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

                if (sus != null &&
                    SuscripcionBeneficiosHelper.AplicarVencimientoSiCorresponde(sus, now))
                {
                    await _context.SaveChangesAsync();
                }

                return ResultadoOperacion<SuscripcionDto?>.SetExito(
                    sus == null ? null : sus.ToDto(nombresPlanes: await MapaNombresPlanesAsync()));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en SuscripcionServicio.ObtenerMiSuscripcionAsync");
                return ResultadoOperacion<SuscripcionDto?>.SetError("Ocurrió un error inesperado. Intente nuevamente.");
            }

        }

        public async Task<ResultadoOperacion<SuscripcionElegibilidadDto>> ObtenerElegibilidadAsync(Guid usuarioId)
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
                var planEntrada = await SuscripcionBeneficiosHelper.ObtenerPlanEntradaAsync(_context);

                string mensaje;
                if (tieneCanceladaConAcceso)
                    mensaje =
                        "Tu suscripción está cancelada. Tu tienda y productos ya no son visibles para compradores. " +
                        "Reactiva tu plan y completa el pago para volver a vender.";
                else if (requierePago)
                    mensaje =
                        "Al contratar de nuevo no hay meses gratis: deberás pagar el plan para activar la suscripción.";
                else if (planEntrada != null)
                    mensaje =
                        $"Tu primera suscripción al plan {planEntrada.Nombre} incluye " +
                        $"{SuscripcionBeneficiosHelper.MesesPruebaPrimeraVez} meses de prueba gratis. " +
                        "Los demás planes se pagan desde el inicio.";
                else
                    mensaje = "Elige un plan para empezar a vender.";

                return ResultadoOperacion<SuscripcionElegibilidadDto>.SetExito(new SuscripcionElegibilidadDto
                {
                    ElegibleMesesGratis = elegibleMesesGratis,
                    PuedeContratarNueva = puedeContratar,
                    RequierePagoInmediato = requierePago,
                    TieneCanceladaConAcceso = tieneCanceladaConAcceso,
                    Mensaje = mensaje,
                    PlanPruebaId = planEntrada?.PlanId,
                    PlanPruebaNombre = planEntrada?.Nombre,
                    MesesPrueba = SuscripcionBeneficiosHelper.MesesPruebaPrimeraVez,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en SuscripcionServicio.ObtenerElegibilidadAsync");
                return ResultadoOperacion<SuscripcionElegibilidadDto>.SetError("Ocurrió un error inesperado. Intente nuevamente.");
            }

        }

        public async Task<ResultadoOperacion<SuscripcionDto>> IniciarAsync(Guid usuarioId, IniciarSuscripcionDto dto)
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
                _logger.LogError(ex, "Error en SuscripcionServicio.IniciarAsync");
                return ResultadoOperacion<SuscripcionDto>.SetError("Ocurrió un error inesperado. Intente nuevamente.");
            }

        }

        public async Task<ResultadoOperacion<SuscripcionDto>> ReactivarPlanAsync(Guid usuarioId, IniciarSuscripcionDto dto)
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
                _logger.LogError(ex, "Error en SuscripcionServicio.ReactivarPlanAsync");
                return ResultadoOperacion<SuscripcionDto>.SetError("Ocurrió un error inesperado. Intente nuevamente.");
            }

        }

        public async Task<ResultadoOperacion<CotizacionCambioPlanDto>> CotizarCambioPlanAsync(
            Guid usuarioId, CotizarCambioPlanDto dto)
        {
            try
            {
                var vendedor = await _context.Vendedores.AsNoTracking()
                    .FirstOrDefaultAsync(v => v.UsuarioId == usuarioId);
                if (vendedor == null)
                    return ResultadoOperacion<CotizacionCambioPlanDto>.SetError("No tienes perfil de vendedor.");

                var now = DateTime.UtcNow;
                var sus = await ObtenerSuscripcionGestionableAsync(vendedor.VendedorId);
                if (sus == null)
                    return ResultadoOperacion<CotizacionCambioPlanDto>.SetError("No tienes una suscripción activa.");

                if (SuscripcionBeneficiosHelper.AplicarVencimientoSiCorresponde(sus, now))
                    await _context.SaveChangesAsync();

                var (nuevoPlan, error) = await ObtenerPlanDestinoAsync(sus, dto.NuevoPlanId);
                if (nuevoPlan == null)
                    return ResultadoOperacion<CotizacionCambioPlanDto>.SetError(error!);

                var cotizacion = CambioPlanCalculadora.Cotizar(sus, nuevoPlan, now);
                var enUso = await LimiteProductosPlanHelper.ContarEnUsoAsync(_context, vendedor.VendedorId);
                var excedeCupo = nuevoPlan.MaxProductos.HasValue && enUso > nuevoPlan.MaxProductos.Value;

                if (excedeCupo && cotizacion.RequierePago)
                    return ResultadoOperacion<CotizacionCambioPlanDto>.SetError(
                        $"El plan {nuevoPlan.Nombre} permite {nuevoPlan.MaxProductos} productos y tienes {enUso}. " +
                        "Pausa productos antes de cambiar.");

                var finAcceso = SuscripcionBeneficiosHelper.FechaFinAcceso(sus);
                var diasRestantes = finAcceso.HasValue && finAcceso > now
                    ? (int)(finAcceso.Value - now).TotalDays
                    : 0;
                var precioNuevo = SuscripcionBeneficiosHelper.PrecioPorPeriodo(nuevoPlan, null, sus.Cupon);

                return ResultadoOperacion<CotizacionCambioPlanDto>.SetExito(new CotizacionCambioPlanDto
                {
                    SuscripcionId = sus.SuscripcionId,
                    PlanActualId = sus.PlanId,
                    PlanActualNombre = sus.Plan.Nombre,
                    NuevoPlanId = nuevoPlan.PlanId,
                    NuevoPlanNombre = nuevoPlan.Nombre,
                    Tipo = new EnumeracionDto
                    {
                        Id = (int)cotizacion.Tipo,
                        Nombre = cotizacion.Tipo.GetDescription(),
                    },
                    RequierePago = cotizacion.RequierePago,
                    Monto = cotizacion.Monto,
                    PrecioNuevo = precioNuevo,
                    TerminaPrueba = cotizacion.MotivoCobro == TipoMotivoPagoSuscripcion.UpgradeDesdePrueba,
                    DiasRestantes = diasRestantes,
                    Resumen = ResumenCambio(sus, nuevoPlan, cotizacion, precioNuevo, diasRestantes),
                    ProductosEnUso = enUso,
                    MaxProductosNuevo = nuevoPlan.MaxProductos,
                    RequiereSeleccionProductos = excedeCupo,
                    Productos = excedeCupo
                        ? await ListarProductosEnUsoAsync(vendedor.VendedorId)
                        : new List<ProductoCupoPlanDto>(),
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en SuscripcionServicio.CotizarCambioPlanAsync");
                return ResultadoOperacion<CotizacionCambioPlanDto>.SetError("Ocurrió un error inesperado. Intente nuevamente.");
            }
        }

        /// <summary>
        /// Aplica de inmediato los cambios sin cobro (downgrade, upgrade sin diferencia o con pago pendiente).
        /// Los upgrades con cobro se completan en el checkout y se aplican al confirmarse el pago.
        /// </summary>
        public async Task<ResultadoOperacion<SuscripcionDto>> CambiarPlanAsync(Guid usuarioId, CambiarPlanDto dto)
        {
            await using var trx = await _context.Database.BeginTransactionAsync();
            try
            {
                var vendedor = await _context.Vendedores.FirstOrDefaultAsync(v => v.UsuarioId == usuarioId);
                if (vendedor == null)
                    return ResultadoOperacion<SuscripcionDto>.SetError("No tienes perfil de vendedor.");

                var now = DateTime.UtcNow;
                var sus = await ObtenerSuscripcionGestionableAsync(vendedor.VendedorId);
                if (sus == null)
                    return ResultadoOperacion<SuscripcionDto>.SetError("No tienes una suscripción activa.");

                SuscripcionBeneficiosHelper.AplicarVencimientoSiCorresponde(sus, now);

                var (nuevoPlan, error) = await ObtenerPlanDestinoAsync(sus, dto.NuevoPlanId);
                if (nuevoPlan == null)
                    return ResultadoOperacion<SuscripcionDto>.SetError(error!);

                var cobroEnCurso = await _context.Transacciones.AnyAsync(t =>
                    t.SuscripcionId == sus.SuscripcionId &&
                    t.Tipo == TipoTransaccion.PagoSuscripcion &&
                    t.Estado == TipoEstadoTransaccion.Procesando);
                if (cobroEnCurso)
                    return ResultadoOperacion<SuscripcionDto>.SetError(
                        "Tienes un pago en verificación. Espera a que se confirme antes de cambiar de plan.");

                var cotizacion = CambioPlanCalculadora.Cotizar(sus, nuevoPlan, now);
                if (cotizacion.RequierePago)
                    return ResultadoOperacion<SuscripcionDto>.SetError(
                        "Este cambio requiere un pago. Continúa al checkout para completarlo.");

                var pausados = 0;
                if (nuevoPlan.MaxProductos is int max)
                {
                    var enUso = await _context.Productos
                        .Where(p => p.VendedorId == vendedor.VendedorId &&
                                    (p.Estado == TipoEstadoProducto.Activo ||
                                     p.Estado == TipoEstadoProducto.Borrador))
                        .ToListAsync();

                    if (enUso.Count > max)
                    {
                        if (dto.ProductoIdsConservar == null)
                            return ResultadoOperacion<SuscripcionDto>.SetError(
                                $"El plan {nuevoPlan.Nombre} permite {max} productos y tienes {enUso.Count}. " +
                                "Elige cuáles conservar.");

                        var conservar = dto.ProductoIdsConservar.ToHashSet();
                        if (conservar.Count > max)
                            return ResultadoOperacion<SuscripcionDto>.SetError(
                                $"Solo puedes conservar hasta {max} productos en el plan {nuevoPlan.Nombre}.");

                        if (!conservar.IsSubsetOf(enUso.Select(p => p.ProductoId)))
                            return ResultadoOperacion<SuscripcionDto>.SetError(
                                "La selección incluye productos que no son tuyos o ya están pausados.");

                        foreach (var producto in enUso.Where(p => !conservar.Contains(p.ProductoId)))
                        {
                            producto.Estado = TipoEstadoProducto.Pausado;
                            pausados++;
                        }
                    }
                }

                var planAnterior = sus.Plan.Nombre;
                CambioPlanCalculadora.AplicarPlan(sus, nuevoPlan);
                await _context.SaveChangesAsync();
                await trx.CommitAsync();

                var cuerpo = $"Tu plan cambió de {planAnterior} a {nuevoPlan.Nombre}.";
                if (pausados > 0)
                    cuerpo += $" Pausamos {pausados} producto(s) para ajustarte al nuevo límite.";

                await _notificacionServicio.CrearAsync(
                    usuarioId,
                    TipoNotificacion.SuscripcionPlanCambiado,
                    "Plan actualizado",
                    cuerpo);

                return await CargarSuscripcionAsync(sus.SuscripcionId);
            }
            catch (Exception ex)
            {
                await trx.RollbackAsync();
                _logger.LogError(ex, "Error en SuscripcionServicio.CambiarPlanAsync");
                return ResultadoOperacion<SuscripcionDto>.SetError("Ocurrió un error inesperado. Intente nuevamente.");
            }

        }

        public async Task<ResultadoOperacion<bool>> CancelarAsync(Guid usuarioId)
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

                await _notificacionServicio.CrearAsync(
                    usuarioId,
                    TipoNotificacion.SuscripcionCancelada,
                    "Suscripción cancelada",
                    "Tu tienda y productos dejarán de mostrarse al público. " +
                    "Para volver a vender debes reactivar el plan y pagar (sin meses gratis).");

                return ResultadoOperacion<bool>.SetExito(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en SuscripcionServicio.CancelarAsync");
                return ResultadoOperacion<bool>.SetError("Ocurrió un error inesperado. Intente nuevamente.");
            }

        }

        public async Task<bool> PuedeVendedorPublicarAsync(int vendedorId)
        {
            var now = DateTime.UtcNow;
            var activas = await _context.Suscripciones.AsNoTracking()
                .Where(s => s.VendedorId == vendedorId &&
                            (s.Estado == TipoEstadoSuscripcion.EnPrueba ||
                             s.Estado == TipoEstadoSuscripcion.Activa))
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
                _logger.LogError(ex, "Error en SuscripcionServicio.ListarAdminAsync");
                return ResultadoOperacion<PaginacionRespuestaDto<SuscripcionDto>>.SetError("Ocurrió un error inesperado. Intente nuevamente.");
            }

        }

        public async Task<ResultadoOperacion<bool>> SuspenderAsync(int suscripcionId)
        {
            try
            {
                var sus = await _context.Suscripciones.FirstOrDefaultAsync(x => x.SuscripcionId == suscripcionId);
                if (sus == null)
                    return ResultadoOperacion<bool>.SetError("Suscripción no encontrada.");
                sus.Estado = TipoEstadoSuscripcion.Suspendida;
                await _context.SaveChangesAsync();
                return ResultadoOperacion<bool>.SetExito(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en SuscripcionServicio.SuspenderAsync");
                return ResultadoOperacion<bool>.SetError("Ocurrió un error inesperado. Intente nuevamente.");
            }
        }

        public async Task<ResultadoOperacion<bool>> ReactivarAsync(int suscripcionId)
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en SuscripcionServicio.ReactivarAsync");
                return ResultadoOperacion<bool>.SetError("Ocurrió un error inesperado. Intente nuevamente.");
            }
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
                _logger.LogError(ex, "Error en SuscripcionServicio.ProcesarVencimientosAsync");
                return ResultadoOperacion<int>.SetError("Ocurrió un error inesperado. Intente nuevamente.");
            }
        }

        public async Task<ResultadoOperacion<int>> EnviarAvisosVencimientoAsync()
        {
            try
            {
                var now = DateTime.UtcNow;
                var limite = now.AddDays(SuscripcionBeneficiosHelper.DiasVentanaRenovacion);

                var porVencer = await _context.Suscripciones
                    .Include(s => s.Plan)
                    .Include(s => s.Cupon)
                    .Include(s => s.Vendedor)
                    .Where(s => s.AvisoVencimientoEnviadoEn == null &&
                                ((s.Estado == TipoEstadoSuscripcion.EnPrueba &&
                                  s.PruebaTerminaEn > now && s.PruebaTerminaEn <= limite) ||
                                 (s.Estado == TipoEstadoSuscripcion.Activa &&
                                  s.PeriodoFin > now && s.PeriodoFin <= limite)))
                    .ToListAsync();

                if (porVencer.Count == 0)
                    return ResultadoOperacion<int>.SetExito(0);

                // Se marca antes de notificar: si el ciclo se repite no se duplican avisos.
                foreach (var s in porVencer)
                    s.AvisoVencimientoEnviadoEn = now;
                await _context.SaveChangesAsync();

                foreach (var s in porVencer)
                {
                    var vence = SuscripcionBeneficiosHelper.FechaFinAcceso(s)!.Value;
                    var dias = Math.Max(1, (int)Math.Ceiling((vence - now).TotalDays));
                    var detalle = s.Estado == TipoEstadoSuscripcion.EnPrueba
                        ? $"Tu prueba gratis del plan {s.Plan.Nombre} termina en {dias} día(s). " +
                          "Si pagas ahora, el periodo pagado se suma al final de tu prueba."
                        : $"Tu plan {s.Plan.Nombre} vence en {dias} día(s). " +
                          "Renueva ahora para que tu tienda siga visible; el periodo pagado se suma a tu fecha actual.";

                    await _notificacionServicio.CrearAsync(
                        s.Vendedor.UsuarioId,
                        TipoNotificacion.SuscripcionPorVencer,
                        "Tu plan está por vencer",
                        detalle,
                        plantillaEmail: PlantillaCorreo.SuscripcionPorVencer,
                        placeholdersEmail: new Dictionary<string, string>
                        {
                            ["vendedor"] = s.Vendedor.NombreTienda,
                            ["plan"] = s.Plan.Nombre,
                            ["venceEl"] = SuscripcionBeneficiosHelper.FormatoFechaPeru(vence),
                            ["monto"] = SuscripcionBeneficiosHelper.FormatoSoles(
                                SuscripcionBeneficiosHelper.PrecioPorPeriodo(s.Plan, s.PrecioPersonalizado, s.Cupon)),
                            ["detalle"] = detalle,
                        });
                }

                return ResultadoOperacion<int>.SetExito(porVencer.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en SuscripcionServicio.EnviarAvisosVencimientoAsync");
                return ResultadoOperacion<int>.SetError("Ocurrió un error inesperado. Intente nuevamente.");
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
            await _context.Suscripciones
                .Include(s => s.Plan)
                .Include(s => s.Cupon)
                .OrderByDescending(s => s.SuscripcionId)
                .FirstOrDefaultAsync(s => s.VendedorId == vendedorId &&
                    (s.Estado == TipoEstadoSuscripcion.EnPrueba ||
                     s.Estado == TipoEstadoSuscripcion.Activa ||
                     s.Estado == TipoEstadoSuscripcion.PendientePago));

        private async Task<(Plan? Plan, string? Error)> ObtenerPlanDestinoAsync(Suscripcion sus, int nuevoPlanId)
        {
            if (sus.PlanId == nuevoPlanId)
                return (null, "Ya estás en ese plan.");

            var plan = await _context.Planes.FirstOrDefaultAsync(p => p.PlanId == nuevoPlanId && p.Activo);
            return plan == null ? (null, "Plan no encontrado o inactivo.") : (plan, null);
        }

        private async Task<List<ProductoCupoPlanDto>> ListarProductosEnUsoAsync(int vendedorId)
        {
            var filas = await _context.Productos.AsNoTracking()
                .Where(p => p.VendedorId == vendedorId &&
                            (p.Estado == TipoEstadoProducto.Activo || p.Estado == TipoEstadoProducto.Borrador))
                .OrderBy(p => p.Estado == TipoEstadoProducto.Activo ? 0 : 1)
                .ThenByDescending(p => p.ProductoId)
                .Select(p => new
                {
                    p.ProductoId,
                    p.Nombre,
                    p.Estado,
                    ImagenUrl = p.Imagenes
                        .OrderByDescending(i => i.EsPrincipal)
                        .ThenBy(i => i.Orden)
                        .Select(i => i.Url)
                        .FirstOrDefault(),
                })
                .ToListAsync();

            return filas.Select(f => new ProductoCupoPlanDto
            {
                ProductoId = f.ProductoId,
                Nombre = f.Nombre,
                Estado = new EnumeracionDto { Id = (int)f.Estado, Nombre = f.Estado.GetDescription() },
                ImagenUrl = f.ImagenUrl,
            }).ToList();
        }

        private static string ResumenCambio(
            Suscripcion sus,
            Plan nuevoPlan,
            CotizacionCambioPlan cotizacion,
            decimal precioNuevo,
            int diasRestantes)
        {
            var precio = SuscripcionBeneficiosHelper.FormatoSoles(precioNuevo);
            var monto = SuscripcionBeneficiosHelper.FormatoSoles(cotizacion.Monto);

            return cotizacion.MotivoCobro switch
            {
                TipoMotivoPagoSuscripcion.UpgradeDesdePrueba =>
                    $"Tu prueba gratis termina al confirmar el pago. Pagas {monto} y el plan {nuevoPlan.Nombre} " +
                    "queda activo por un periodo completo desde hoy.",
                TipoMotivoPagoSuscripcion.UpgradeProrrateo =>
                    $"Pagas solo la diferencia por los {diasRestantes} días que quedan de tu periodo: {monto}. " +
                    $"Tu fecha de renovación no cambia; desde entonces pagarás {precio} por periodo.",
                _ when sus.Estado == TipoEstadoSuscripcion.PendientePago =>
                    $"El plan cambia ahora. Al completar tu pago pendiente pagarás {precio} por periodo.",
                _ when cotizacion.Tipo == TipoCambioPlan.Upgrade =>
                    $"La diferencia por los días restantes es mínima: el cambio a {nuevoPlan.Nombre} es inmediato y sin costo.",
                _ when sus.Estado == TipoEstadoSuscripcion.EnPrueba =>
                    $"El cambio es inmediato y sigues en tu prueba gratis. Al terminar pagarás {precio} por periodo.",
                _ =>
                    "El cambio es inmediato y conservas tu fecha de renovación. No hay reembolso del periodo actual; " +
                    $"desde la renovación pagarás {precio} por periodo.",
            };
        }

        private async Task<ResultadoOperacion<SuscripcionDto>> CargarSuscripcionAsync(int suscripcionId)
        {
            var s = await _context.Suscripciones.AsNoTracking()
                .Include(x => x.Plan)
                .Include(x => x.Cupon)
                .FirstAsync(x => x.SuscripcionId == suscripcionId);
            var nombres = await MapaNombresPlanesAsync();
            return ResultadoOperacion<SuscripcionDto>.SetExito(s.ToDto(nombresPlanes: nombres));
        }

        private async Task<Dictionary<int, string>> MapaNombresPlanesAsync() =>
            await _context.Planes.AsNoTracking()
                .Select(p => new { p.PlanId, p.Nombre })
                .ToDictionaryAsync(x => x.PlanId, x => x.Nombre);

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
            Guid usuarioId,
            Vendedor vendedor,
            IniciarSuscripcionDto dto,
            bool forzarPago,
            Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction trx)
        {
            var now = DateTime.UtcNow;
            var plan = await _context.Planes.FirstOrDefaultAsync(p => p.PlanId == dto.PlanId && p.Activo);
            if (plan == null)
                return ResultadoOperacion<SuscripcionDto>.SetError("Plan no encontrado o inactivo.");

            var planEntrada = await SuscripcionBeneficiosHelper.ObtenerPlanEntradaAsync(_context);
            var esPlanEntrada = planEntrada?.PlanId == plan.PlanId;
            var elegibleMesesGratis = !forzarPago && esPlanEntrada &&
                await SuscripcionBeneficiosHelper.ElegibleMesesGratisAsync(_context, vendedor.VendedorId);

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

                if (cupon.TipoDescuento == TipoDescuentoCupon.MesesGratis && !esPlanEntrada)
                    return ResultadoOperacion<SuscripcionDto>.SetError(
                        $"Los cupones de meses gratis solo aplican al plan {planEntrada?.Nombre ?? "de entrada"}.");
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

            var tipoNotif = forzarPago ? TipoNotificacion.SuscripcionReactivada : TipoNotificacion.SuscripcionIniciada;
            var titulo = forzarPago
                ? $"Plan {plan.Nombre} — reactivación"
                : $"¡Suscripción al plan {plan.Nombre} iniciada!";
            var cuerpo = estado == TipoEstadoSuscripcion.EnPrueba
                ? $"Tienes {mesesGratisTotal} mes(es) gratis hasta {pruebaTerminaEn:dd/MM/yyyy}."
                : "Tu suscripción está pendiente de pago. Completa el pago para activar tu plan y publicar productos.";

            await trx.CommitAsync();

            await _notificacionServicio.CrearAsync(usuarioId, tipoNotif, titulo, cuerpo);

            return await CargarSuscripcionAsync(suscripcion.SuscripcionId);
        }
    }
}
