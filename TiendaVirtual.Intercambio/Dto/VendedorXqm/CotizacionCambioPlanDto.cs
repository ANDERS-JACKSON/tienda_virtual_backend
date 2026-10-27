using TiendaVirtual.Intercambio.Dto.Sistema;

namespace TiendaVirtual.Intercambio.Dto.VendedorXqm
{
    public class CotizacionCambioPlanDto
    {
        public int SuscripcionId { get; set; }
        public int PlanActualId { get; set; }
        public string PlanActualNombre { get; set; } = null!;
        public int NuevoPlanId { get; set; }
        public string NuevoPlanNombre { get; set; } = null!;

        /// <summary>UPGRADE | DOWNGRADE</summary>
        public EnumeracionDto Tipo { get; set; } = null!;

        /// <summary>True: hay que pasar por el checkout; el plan cambia al confirmarse el pago.</summary>
        public bool RequierePago { get; set; }

        /// <summary>Monto a cobrar ahora (0 si no requiere pago).</summary>
        public decimal Monto { get; set; }

        /// <summary>Precio por periodo del plan nuevo (con cupón si aplica).</summary>
        public decimal PrecioNuevo { get; set; }

        public bool TerminaPrueba { get; set; }
        public int DiasRestantes { get; set; }
        public string Resumen { get; set; } = null!;

        public int ProductosEnUso { get; set; }
        public int? MaxProductosNuevo { get; set; }
        public bool RequiereSeleccionProductos { get; set; }

        /// <summary>Solo cuando hay que elegir qué productos conservar.</summary>
        public List<ProductoCupoPlanDto> Productos { get; set; } = new();
    }

    public class ProductoCupoPlanDto
    {
        public int ProductoId { get; set; }
        public string Nombre { get; set; } = null!;
        public EnumeracionDto Estado { get; set; } = null!;
        public string? ImagenUrl { get; set; }
    }
}
