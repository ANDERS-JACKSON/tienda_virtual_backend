using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.Sistema;
using TiendaVirtual.Intercambio.Dto.VendedorXqm;

namespace TiendaVirtual.Dominio.Servicios.VendedorXqm
{
    public interface IVendedorServicio
    {
        // Perfil propio
        Task<ResultadoOperacion<VendedorPerfilDto>> ObtenerMiPerfilAsync(int usuarioId);
        Task<ResultadoOperacion<VendedorPerfilDto>> ActualizarMiPerfilAsync(int usuarioId, ActualizarPerfilVendedorDto dto);
        Task<ResultadoOperacion<VendedorPerfilDto>> ActualizarImagenesPerfilAsync(int usuarioId, ActualizarImagenesPerfilVendedorDto dto);

        Task<ResultadoOperacion<ElegibilidadCreacionProductoDto>> ObtenerElegibilidadCreacionProductoAsync(int usuarioId);

        // Solicitud de verificación
        Task<ResultadoOperacion<SolicitudVerificacionDto>> EnviarSolicitudVerificacionAsync(int usuarioId, EnviarSolicitudVerificacionDto dto);
        Task<ResultadoOperacion<SolicitudVerificacionDto?>> ObtenerMiSolicitudActualAsync(int usuarioId);

        // Resolución (admin / verificador)
        Task<ResultadoOperacion<PaginacionRespuestaDto<SolicitudVerificacionDto>>> ListarSolicitudesPendientesAsync(int pagina, int tamanioPagina);
        Task<ResultadoOperacion<bool>> AprobarSolicitudAsync(int solicitudId, int verificadorUsuarioId, ResolverSolicitudDto dto);
        Task<ResultadoOperacion<bool>> RechazarSolicitudAsync(int solicitudId, int verificadorUsuarioId, ResolverSolicitudDto dto);

        // Listado público
        Task<ResultadoOperacion<PaginacionRespuestaDto<TiendaPublicaDto>>> ListarTiendasPublicasAsync(int pagina, int tamanioPagina, string? busqueda);
        Task<ResultadoOperacion<TiendaPublicaDto>> ObtenerTiendaPorSlugAsync(string slug);

        // Historias públicas (biografías de vendedores)
        Task<ResultadoOperacion<PaginacionRespuestaDto<HistoriaPublicaListadoDto>>> ListarHistoriasPublicasAsync(int pagina, int tamanioPagina, string? busqueda);
        Task<ResultadoOperacion<HistoriaPublicaDetalleDto>> ObtenerHistoriaPorSlugAsync(string slug);

        // Pedidos del vendedor
        Task<ResultadoOperacion<PaginacionRespuestaDto<PedidoVendedorDto>>> ListarMisPedidosAsync(int usuarioId, TipoEstadoSuborden? estado, int pagina, int tamanioPagina);
        Task<ResultadoOperacion<PedidoVendedorDetalleDto>> ObtenerMisPedidoDetalleAsync(int usuarioId, long subordenId);

        // Admin overview
        Task<ResultadoOperacion<PaginacionRespuestaDto<VendedorAdminListadoDto>>> ListarAdminAsync(
            string? busqueda, TipoEstadoVendedor? estado, bool? conSuscripcion, int pagina, int tamanioPagina);
        Task<ResultadoOperacion<VendedorAdminDetalleDto>> ObtenerAdminDetalleAsync(int vendedorId);
        Task<ResultadoOperacion<bool>> SuspenderAdminAsync(int vendedorId, SuspenderVendedorDto dto);
        Task<ResultadoOperacion<bool>> ReactivarAdminAsync(int vendedorId);
    }
}
