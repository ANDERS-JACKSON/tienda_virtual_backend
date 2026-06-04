namespace TiendaVirtual.Intercambio.Dto.SeguridadXqm
{
    /// <summary>
    /// Respuesta del registro de cliente / vendedor cuando la contraseña se
    /// genera automáticamente en el servidor y se envía por correo. No se
    /// emiten tokens — el usuario debe iniciar sesión con la clave recibida.
    /// </summary>
    public class RegistroRespuestaDto
    {
        public int UsuarioId { get; set; }
        public string Correo { get; set; } = null!;
        public string NombreCompleto { get; set; } = null!;
        public string Mensaje { get; set; } = null!;

        /// <summary>
        /// Indica si la contraseña se envió correctamente al correo del usuario.
        /// El registro se persiste igual; si es <c>false</c> el front muestra un
        /// aviso para que el admin entregue la clave manualmente.
        /// </summary>
        public bool CorreoEnviado { get; set; }
    }
}
