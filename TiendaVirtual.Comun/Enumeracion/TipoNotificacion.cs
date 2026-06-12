namespace TiendaVirtual.Comun.Enumeracion
{
    /// <summary>
    /// Constantes string usadas como discriminador del campo `tipo` en notificacion.
    /// </summary>
    public static class TipoNotificacion
    {
        public const string CreacionUsuario = "CREACION_USUARIO";
        public const string RecuperacionClaveSolicitada = "RECUPERACION_CLAVE_SOLICITADA";

        public const string OrdenCanceladaAdmin = "ORDEN_CANCELADA_ADMIN";
        public const string VendedorSuspendido = "VENDEDOR_SUSPENDIDO";
        public const string ProductoOcultoAdmin = "PRODUCTO_OCULTO_ADMIN";

        public const string VerificacionAprobada = "VERIFICACION_APROBADA";
        public const string VerificacionRechazada = "VERIFICACION_RECHAZADA";
        public const string SolicitudVerificacionRecibida = "SOLICITUD_VERIFICACION_RECIBIDA";

        public const string SuscripcionIniciada = "SUSCRIPCION_INICIADA";
        public const string SuscripcionReactivada = "SUSCRIPCION_REACTIVADA";
        public const string SuscripcionPlanCambiado = "SUSCRIPCION_PLAN_CAMBIADO";
        public const string SuscripcionCancelada = "SUSCRIPCION_CANCELADA";
        public const string SuscripcionPagada = "SUSCRIPCION_PAGADA";
        public const string SuscripcionPorVencer = "SUSCRIPCION_POR_VENCER";
        public const string SuscripcionVencida = "SUSCRIPCION_VENCIDA";

        public const string OrdenCreada = "ORDEN_CREADA";
        public const string OrdenPagada = "ORDEN_PAGADA";
        public const string SubordenRecibida = "SUBORDEN_RECIBIDA";
        public const string SubordenEnPreparacion = "SUBORDEN_EN_PREPARACION";
        public const string SubordenEnCamino = "SUBORDEN_EN_CAMINO";
        public const string SubordenEntregada = "SUBORDEN_ENTREGADA";
        public const string SubordenCancelada = "SUBORDEN_CANCELADA";

        public const string ReclamoAbierto = "RECLAMO_ABIERTO";
        public const string ReclamoMensajeNuevo = "RECLAMO_MENSAJE_NUEVO";
        public const string ReclamoResuelto = "RECLAMO_RESUELTO";

        public const string ResenaRecibida = "RESENA_RECIBIDA";
        public const string ResenaRespondida = "RESENA_RESPONDIDA";
    }
}
