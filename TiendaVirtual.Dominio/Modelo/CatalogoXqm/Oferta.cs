using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Dominio.Utilidad;
using TiendaVirtual.Intercambio;

namespace TiendaVirtual.Dominio.Modelo.CatalogoXqm
{
    public class Oferta
    {
        public int OfertaId { get; set; }

        [Required]
        public int ProductoId { get; set; }

        public string? Nombre { get; set; }
        public decimal? PorcentajeDescuento { get; set; }
        public decimal? PrecioOferta { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public bool Activa { get; set; }

        public virtual Producto Producto { get; set; } = null!;

        public ResultadoOperacion<bool> Validar()
        {
            var respuesta = EntidadValidador.ValidarCamposRequeridos(this);
            if (respuesta.Exito)
            {
                if (PorcentajeDescuento == null && PrecioOferta == null)
                    return ResultadoOperacion<bool>.SetError("Debe indicar porcentaje de descuento o precio de oferta.");
                if (FechaFin <= FechaInicio)
                    return ResultadoOperacion<bool>.SetError("La fecha fin debe ser mayor que la fecha inicio.");
            }
            return respuesta;
        }
    }
}
