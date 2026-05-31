using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiendaVirtual.Intercambio.Dto.SoporteXqm
{
    public class ResenaProductoDto
    {
        public long ResenaId { get; set; }
        public int ProductoId { get; set; }
        public long ItemOrdenId { get; set; }
        public int ClienteId { get; set; }
        public short Calificacion { get; set; }
        public string? Titulo { get; set; }
        public string? Comentario { get; set; }
        public string? Imagenes { get; set; }
        public string? RespuestaVendedor { get; set; }
        public DateTime Fecha { get; set; }
    }
}
