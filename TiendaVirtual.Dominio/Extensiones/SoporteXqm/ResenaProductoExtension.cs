using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Dominio.Modelo.SoporteXqm;
using TiendaVirtual.Intercambio.Dto.SoporteXqm;

namespace TiendaVirtual.Dominio.Extensiones.SoporteXqm
{
    public static class ResenaProductoExtension
    {
        public static ResenaProducto ToEntidad(this ResenaProductoDto dto)
        {
            if (dto == null)
                return null!;

            var resena = new ResenaProducto();

            resena.ResenaId = dto.ResenaId;
            resena.ProductoId = dto.ProductoId;
            resena.ItemOrdenId = dto.ItemOrdenId;
            resena.ClienteId = dto.ClienteId;
            resena.Calificacion = dto.Calificacion;
            resena.Titulo = dto.Titulo?.Normalizar_null();
            resena.Comentario = dto.Comentario?.Normalizar_null();
            resena.Imagenes = dto.Imagenes?.Normalizar_null();
            resena.RespuestaVendedor = dto.RespuestaVendedor?.Normalizar_null();
            resena.Fecha = dto.Fecha;

            return resena;
        }

        public static ResenaProductoDto ToDto(this ResenaProducto entidad)
        {
            if (entidad == null)
                return null!;

            var dto = new ResenaProductoDto();

            dto.ResenaId = entidad.ResenaId;
            dto.ProductoId = entidad.ProductoId;
            dto.ItemOrdenId = entidad.ItemOrdenId;
            dto.ClienteId = entidad.ClienteId;
            dto.Calificacion = entidad.Calificacion;
            dto.Titulo = entidad.Titulo;
            dto.Comentario = entidad.Comentario;
            dto.Imagenes = entidad.Imagenes;
            dto.RespuestaVendedor = entidad.RespuestaVendedor;
            dto.Fecha = entidad.Fecha;

            return dto;
        }
    }
}
