using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.Sistema;

namespace TiendaVirtual.Dominio.Servicios.Sistema.Implementacion
{
    public class EnumeracionServicio : IEnumeracionServicio
    {
        private static readonly IReadOnlyDictionary<string, Func<IReadOnlyList<EnumeracionDto>>> Catalogo =
            new Dictionary<string, Func<IReadOnlyList<EnumeracionDto>>>(StringComparer.OrdinalIgnoreCase)
            {
                ["tipo-documento-identidad"] = () => ListarEnum<TipoDocumentoIdentidad>(),
            };

        public Task<ResultadoOperacion<List<EnumeracionDto>>> ListarPorGrupoAsync(string grupo)
        {
            if (string.IsNullOrWhiteSpace(grupo))
                return Task.FromResult(ResultadoOperacion<List<EnumeracionDto>>.SetError("Grupo requerido."));

            if (!Catalogo.TryGetValue(grupo.Trim(), out var factory))
                return Task.FromResult(ResultadoOperacion<List<EnumeracionDto>>.SetError("Grupo de enumeración no reconocido."));

            return Task.FromResult(ResultadoOperacion<List<EnumeracionDto>>.SetExito(factory().ToList()));
        }

        public Task<ResultadoOperacion<List<TipoDocumentoIdentidadDto>>> ListarTiposDocumentoIdentidadAsync()
        {
            var items = Enum.GetValues<TipoDocumentoIdentidad>()
                .Select(MapTipoDocumento)
                .OrderBy(x => x.Id)
                .ToList();

            return Task.FromResult(ResultadoOperacion<List<TipoDocumentoIdentidadDto>>.SetExito(items));
        }

        private static IReadOnlyList<EnumeracionDto> ListarEnum<TEnum>() where TEnum : struct, Enum =>
            Enum.GetValues<TEnum>()
                .Cast<TEnum>()
                .Select(valor => new EnumeracionDto(Convert.ToInt32(valor), valor.GetDescription()))
                .OrderBy(dto => dto.Id)
                .ToList();

        private static TipoDocumentoIdentidadDto MapTipoDocumento(TipoDocumentoIdentidad tipo) =>
            new()
            {
                Id = (int)tipo,
                Nombre = tipo.GetDescription(),
                Etiqueta = ObtenerEtiquetaDocumento(tipo),
                LongitudDocumento = ObtenerLongitudDocumento(tipo),
            };

        private static string ObtenerEtiquetaDocumento(TipoDocumentoIdentidad tipo) => tipo switch
        {
            TipoDocumentoIdentidad.DNI => "DNI",
            TipoDocumentoIdentidad.CarnetExtranjeria => "Carnet de extranjería",
            TipoDocumentoIdentidad.Pasaporte => "Pasaporte",
            _ => tipo.GetDescription(),
        };

        private static int ObtenerLongitudDocumento(TipoDocumentoIdentidad tipo) => tipo switch
        {
            TipoDocumentoIdentidad.DNI => 8,
            TipoDocumentoIdentidad.CarnetExtranjeria => 12,
            TipoDocumentoIdentidad.Pasaporte => 9,
            _ => 0,
        };
    }
}
