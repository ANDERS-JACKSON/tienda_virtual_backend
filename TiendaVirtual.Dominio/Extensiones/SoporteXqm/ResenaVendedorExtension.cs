using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Dominio.Modelo.SoporteXqm;
using TiendaVirtual.Intercambio.Dto.SoporteXqm;

namespace TiendaVirtual.Dominio.Extensiones.SoporteXqm
{
    public static class ResenaVendedorExtension
    {
        public static ResenaVendedor ToEntidad(this ResenaVendedorDto dto)
        {
            if (dto == null)
                return null!;

            var resena = new ResenaVendedor();

            resena.ResenaId = dto.ResenaId;
            resena.VendedorId = dto.VendedorId;
            resena.SubordenId = dto.SubordenId;
            resena.ClienteId = dto.ClienteId;
            resena.Calificacion = dto.Calificacion;
            resena.Comentario = dto.Comentario?.Normalizar_null();
            resena.Fecha = dto.Fecha;

            return resena;
        }

        public static ResenaVendedorDto ToDto(this ResenaVendedor entidad)
        {
            if (entidad == null)
                return null!;

            var dto = new ResenaVendedorDto();

            dto.ResenaId = entidad.ResenaId;
            dto.VendedorId = entidad.VendedorId;
            dto.SubordenId = entidad.SubordenId;
            dto.ClienteId = entidad.ClienteId;
            dto.Calificacion = entidad.Calificacion;
            dto.Comentario = entidad.Comentario;
            dto.Fecha = entidad.Fecha;

            return dto;
        }
    }
}
