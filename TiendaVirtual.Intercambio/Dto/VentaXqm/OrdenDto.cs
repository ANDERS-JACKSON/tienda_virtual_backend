using System;
using System.Collections.Generic;
using TiendaVirtual.Intercambio.Dto.SeguridadXqm;
using TiendaVirtual.Intercambio.Dto.Sistema;

namespace TiendaVirtual.Intercambio.Dto.VentaXqm
{
    /// <summary>
    /// Snapshot de la dirección al momento de la orden (deserializa el jsonb).
    /// </summary>
    public class DireccionSnapshotDto
    {
        public string? Etiqueta { get; set; }
        public string NombreReceptor { get; set; } = string.Empty;
        public string? Telefono { get; set; }
        public string Departamento { get; set; } = string.Empty;
        public string Provincia { get; set; } = string.Empty;
        public string Distrito { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
        public string? Referencia { get; set; }
        public string? CodigoPostal { get; set; }
    }

    public class OrdenDto
    {
        public long OrdenId { get; set; }
        public string NumeroOrden { get; set; } = null!;
        public int ClienteId { get; set; }
        public string CorreoCliente { get; set; } = null!;
        public string? TelefonoCliente { get; set; }
        public DireccionSnapshotDto DireccionEnvio { get; set; } = new();
        public decimal Subtotal { get; set; }
        public decimal TotalEnvio { get; set; }
        public decimal TotalDescuento { get; set; }
        public decimal Total { get; set; }
        public EnumeracionDto Estado { get; set; } = null!;
        public DateTime Fecha { get; set; }
        public List<SubordenDto> Subordenes { get; set; } = new();
    }
}
