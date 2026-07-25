using System.Text.Json;
using System.Text.Json.Serialization;
using TiendaVirtual.Comun.Enumeracion;
using TiendaVirtual.Dominio.Modelo.VendedorXqm;
using TiendaVirtual.Intercambio.Dto.Sistema;
using TiendaVirtual.Intercambio.Dto.VendedorXqm;

namespace TiendaVirtual.Dominio.Extensiones.VendedorXqm
{
    public static class PlanExtension
    {
        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true
        };

        public static PlanBeneficiosDto? DeserializarBeneficios(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                var dto = JsonSerializer.Deserialize<PlanBeneficiosDto>(json, JsonOpts);
                if (dto == null) return null;
                dto.Items ??= new List<PlanBeneficioItemDto>();
                foreach (var item in dto.Items)
                {
                    if (item.Texto != null)
                        item.Texto = item.Texto.Trim();
                }
                dto.Items = dto.Items
                    .Where(i => !string.IsNullOrWhiteSpace(i.Texto))
                    .ToList();
                return dto;
            }
            catch
            {
                return null;
            }
        }

        public static string? SerializarBeneficios(PlanBeneficiosDto? dto)
        {
            if (dto == null) return null;

            var limpio = new PlanBeneficiosDto
            {
                Etiqueta = string.IsNullOrWhiteSpace(dto.Etiqueta) ? null : dto.Etiqueta.Trim(),
                HeredaDePlanId = dto.HeredaDePlanId,
                Destacado = dto.Destacado,
                NotaPie = string.IsNullOrWhiteSpace(dto.NotaPie) ? null : dto.NotaPie.Trim(),
                Items = (dto.Items ?? new List<PlanBeneficioItemDto>())
                    .Where(i => !string.IsNullOrWhiteSpace(i.Texto))
                    .Select(i => new PlanBeneficioItemDto
                    {
                        Texto = i.Texto.Trim(),
                        Incluido = i.Incluido
                    })
                    .ToList()
            };

            // No persistir campos solo de lectura
            limpio.EtiquetaResuelta = "Incluye";
            limpio.HeredaDePlanNombre = null;

            return JsonSerializer.Serialize(limpio, JsonOpts);
        }

        public static PlanBeneficiosDto ResolverBeneficios(
            string? json,
            int planId,
            IReadOnlyDictionary<int, string> nombresPorId)
        {
            var dto = DeserializarBeneficios(json) ?? new PlanBeneficiosDto
            {
                Etiqueta = "Incluye",
                Items = new List<PlanBeneficioItemDto>()
            };

            if (dto.HeredaDePlanId is int baseId && baseId != planId
                && nombresPorId.TryGetValue(baseId, out var nombreBase)
                && !string.IsNullOrWhiteSpace(nombreBase))
            {
                dto.HeredaDePlanNombre = nombreBase;
                dto.EtiquetaResuelta = $"Todo lo del {nombreBase}, más";
            }
            else
            {
                dto.HeredaDePlanId = null;
                dto.HeredaDePlanNombre = null;
                dto.EtiquetaResuelta = string.IsNullOrWhiteSpace(dto.Etiqueta)
                    ? "Incluye"
                    : dto.Etiqueta.Trim();
            }

            return dto;
        }

        public static PlanDto ToDto(this Plan entidad, IReadOnlyDictionary<int, string>? nombresPorId = null)
        {
            if (entidad == null) return null!;

            var nombres = nombresPorId ?? new Dictionary<int, string>();

            return new PlanDto
            {
                PlanId = entidad.PlanId,
                Codigo = entidad.Codigo,
                Nombre = entidad.Nombre,
                Descripcion = entidad.Descripcion,
                Precio = entidad.Precio,
                Periodo = new EnumeracionDto
                {
                    Id = (int)entidad.Periodo,
                    Nombre = entidad.Periodo.GetDescription()
                },
                MaxProductos = entidad.MaxProductos,
                TasaComision = entidad.TasaComision,
                Activo = entidad.Activo,
                Beneficios = ResolverBeneficios(entidad.Beneficios, entidad.PlanId, nombres)
            };
        }

        public static Plan ToEntidad(this PlanDto dto)
        {
            if (dto == null) return null!;

            return new Plan
            {
                PlanId = dto.PlanId,
                Codigo = dto.Codigo.Normalizar(),
                Nombre = dto.Nombre.Normalizar(),
                Descripcion = dto.Descripcion?.Normalizar_null(),
                Precio = dto.Precio,
                Periodo = (TipoPeriodoPlan)dto.Periodo.Id,
                MaxProductos = dto.MaxProductos,
                TasaComision = dto.TasaComision,
                Activo = dto.Activo,
                Beneficios = SerializarBeneficios(dto.Beneficios)
            };
        }
    }
}
