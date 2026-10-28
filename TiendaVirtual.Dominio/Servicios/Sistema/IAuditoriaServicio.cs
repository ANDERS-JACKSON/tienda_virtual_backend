using System;
using System.Threading.Tasks;

namespace TiendaVirtual.Dominio.Servicios.Sistema
{
    /// <summary>
    /// Registro de eventos de seguridad. Nunca debe abortar la operación de negocio:
    /// si la tabla no existe o falla el insert, se loguea y se sigue.
    /// </summary>
    public interface IAuditoriaServicio
    {
        Task RegistrarAsync(string accion, Guid? usuarioId, string? direccionIp, string? detalle);
    }
}
