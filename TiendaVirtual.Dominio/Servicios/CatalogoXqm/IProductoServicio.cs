using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.CatalogoXqm;
using TiendaVirtual.Intercambio.Dto.Sistema;

namespace TiendaVirtual.Dominio.Servicios.CatalogoXqm
{
    public interface IProductoServicio
    {
        // Listado y detalle (vendedor)
        Task<ResultadoOperacion<PaginacionRespuestaDto<ProductoDto>>> ListarMisProductosAsync(
            Guid usuarioId, int pagina, int tamanioPagina);
        Task<ResultadoOperacion<ProductoDto>> ObtenerMiProductoAsync(Guid usuarioId, int productoId);

        // CRUD del producto
        Task<ResultadoOperacion<ProductoDto>> CrearAsync(Guid usuarioId, CrearProductoDto dto);
        Task<ResultadoOperacion<ProductoDto>> ActualizarAsync(Guid usuarioId, int productoId, ActualizarProductoDto dto);
        Task<ResultadoOperacion<bool>> EliminarAsync(Guid usuarioId, int productoId);

        // Cambios de estado
        Task<ResultadoOperacion<bool>> PublicarAsync(Guid usuarioId, int productoId);
        Task<ResultadoOperacion<bool>> PausarAsync(Guid usuarioId, int productoId);

        // Variantes
        Task<ResultadoOperacion<VarianteProductoDto>> AgregarVarianteAsync(Guid usuarioId, int productoId, CrearVarianteDto dto);
        Task<ResultadoOperacion<VarianteProductoDto>> ActualizarVarianteAsync(Guid usuarioId, int varianteId, ActualizarVarianteDto dto);
        Task<ResultadoOperacion<bool>> EliminarVarianteAsync(Guid usuarioId, int varianteId);
        Task<ResultadoOperacion<VarianteProductoDto>> ActualizarStockAsync(Guid usuarioId, int varianteId, ActualizarStockDto dto);

        // Imágenes
        Task<ResultadoOperacion<ImagenProductoDto>> AgregarImagenAsync(Guid usuarioId, int productoId, CrearImagenDto dto);
        Task<ResultadoOperacion<ImagenProductoDto>> ActualizarImagenAsync(Guid usuarioId, int imagenId, ActualizarImagenDto dto);
        Task<ResultadoOperacion<bool>> EliminarImagenAsync(Guid usuarioId, int imagenId);

        // Ofertas
        Task<ResultadoOperacion<OfertaDto>> CrearOfertaAsync(Guid usuarioId, int productoId, CrearOfertaDto dto);
        Task<ResultadoOperacion<List<OfertaDto>>> ListarOfertasAsync(Guid usuarioId, int productoId);
        Task<ResultadoOperacion<OfertaDto>> ActualizarOfertaAsync(Guid usuarioId, int ofertaId, ActualizarOfertaDto dto);
        Task<ResultadoOperacion<bool>> ActivarOfertaAsync(Guid usuarioId, int ofertaId);
        Task<ResultadoOperacion<bool>> DesactivarOfertaAsync(Guid usuarioId, int ofertaId);
        Task<ResultadoOperacion<bool>> EliminarOfertaAsync(Guid usuarioId, int ofertaId);

        // Moderación admin
        Task<ResultadoOperacion<PaginacionRespuestaDto<ProductoAdminListadoDto>>> ListarAdminAsync(
            string? busqueda, int? vendedorId, int? categoriaId, string? estado, int pagina, int tamanioPagina);
        Task<ResultadoOperacion<bool>> OcultarAdminAsync(int productoId, OcultarProductoDto dto);
        Task<ResultadoOperacion<bool>> RestaurarAdminAsync(int productoId);
    }
}
