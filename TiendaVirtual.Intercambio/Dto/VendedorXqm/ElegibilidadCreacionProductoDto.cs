namespace TiendaVirtual.Intercambio.Dto.VendedorXqm
{
    /// <summary>
    /// Requisitos del vendedor para iniciar el flujo de creación de productos.
    /// </summary>
    public class ElegibilidadCreacionProductoDto
    {
        public bool CuentaVerificada { get; set; }
        public bool PlanActivo { get; set; }
        public bool PuedeCrearProducto { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        /// <summary>VERIFICACION | PLAN | OK</summary>
        public string CodigoBloqueo { get; set; } = "OK";
    }
}
