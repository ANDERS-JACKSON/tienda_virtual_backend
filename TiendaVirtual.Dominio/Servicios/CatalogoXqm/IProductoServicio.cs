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
            int usuarioId, int pagina, int tamanioPagina);
        Task<ResultadoOperacion<ProductoDto>> ObtenerMiProductoAsync(int usuarioId, int productoId);

        // CRUD del producto
        Task<ResultadoOperacion<ProductoDto>> CrearAsync(int usuarioId, CrearProductoDto dto);
        Task<ResultadoOperacion<ProductoDto>> ActualizarAsync(int usuarioId, int productoId, ActualizarProductoDto dto);
        Task<ResultadoOperacion<bool>> EliminarAsync(int usuarioId, int productoId);

        // Cambios de estado
        Task<ResultadoOperacion<bool>> PublicarAsync(int usuarioId, int productoId);
        Task<ResultadoOperacion<bool>> PausarAsync(int usuarioId, int productoId);

        // Variantes
        Task<ResultadoOperacion<VarianteProductoDto>> AgregarVarianteAsync(int usuarioId, int productoId, CrearVarianteDto dto);
        Task<ResultadoOperacion<VarianteProductoDto>> ActualizarVarianteAsync(int usuarioId, int varianteId, ActualizarVarianteDto dto);
        Task<ResultadoOperacion<bool>> EliminarVarianteAsync(int usuarioId, int varianteId);
        Task<ResultadoOperacion<VarianteProductoDto>> ActualizarStockAsync(int usuarioId, int varianteId, ActualizarStockDto dto);

        // Imágenes
        Task<ResultadoOperacion<ImagenProductoDto>> AgregarImagenAsync(int usuarioId, int productoId, CrearImagenDto dto);
        Task<ResultadoOperacion<ImagenProductoDto>> ActualizarImagenAsync(int usuarioId, int imagenId, ActualizarImagenDto dto);
        Task<ResultadoOperacion<bool>> EliminarImagenAsync(int usuarioId, int imagenId);

        // Ofertas
        Task<ResultadoOperacion<OfertaDto>> CrearOfertaAsync(int usuarioId, int productoId, CrearOfertaDto dto);
        Task<ResultadoOperacion<OfertaDto>> ActualizarOfertaAsync(int usuarioId, int ofertaId, ActualizarOfertaDto dto);
        Task<ResultadoOperacion<bool>> DesactivarOfertaAsync(int usuarioId, int ofertaId);
    }
}
