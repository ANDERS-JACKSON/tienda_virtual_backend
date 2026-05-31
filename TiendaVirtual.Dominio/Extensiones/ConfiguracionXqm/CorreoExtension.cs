using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Dominio.Modelo.ConfiguracionXqm;
using TiendaVirtual.Intercambio.Dto.ConfiguracionXqm;

namespace TiendaVirtual.Dominio.Extensiones.ConfiguracionXqm
{
    public static class CorreoExtension
    {
        public static Correo ToEntidad(this CorreoDto dto)
        {
            if (dto == null)
                return null!;

            var correo = new Correo();

            correo.CorreoId = dto.CorreoId;
            correo.Remitente = dto.Remitente?.Normalizar_null();
            correo.CorreoElectronico = dto.CorreoElectronico?.Normalizar_null();
            correo.Puerto = dto.Puerto;
            correo.Contrasenia = dto.Contrasenia?.Normalizar_null();
            correo.ServidorSmtp = dto.ServidorSmtp?.Normalizar_null();
            correo.AsuntoCreacionUsuario = dto.AsuntoCreacionUsuario?.Normalizar_null();
            correo.CuerpoCreacionUsuario = dto.CuerpoCreacionUsuario?.Normalizar_null();
            correo.AsuntoRecuperacionClave = dto.AsuntoRecuperacionClave?.Normalizar_null();
            correo.CuerpoRecuperacionClave = dto.CuerpoRecuperacionClave?.Normalizar_null();
            correo.AsuntoNuevoPedidoVendedor = dto.AsuntoNuevoPedidoVendedor?.Normalizar_null();
            correo.CuerpoNuevoPedidoVendedor = dto.CuerpoNuevoPedidoVendedor?.Normalizar_null();
            correo.AsuntoPedidoEnviadoCliente = dto.AsuntoPedidoEnviadoCliente?.Normalizar_null();
            correo.CuerpoPedidoEnviadoCliente = dto.CuerpoPedidoEnviadoCliente?.Normalizar_null();
            correo.AsuntoVerificacionResultado = dto.AsuntoVerificacionResultado?.Normalizar_null();
            correo.CuerpoVerificacionResultado = dto.CuerpoVerificacionResultado?.Normalizar_null();

            return correo;
        }

        public static CorreoDto ToDto(this Correo entidad)
        {
            if (entidad == null)
                return null!;

            var dto = new CorreoDto();

            dto.CorreoId = entidad.CorreoId;
            dto.Remitente = entidad.Remitente;
            dto.CorreoElectronico = entidad.CorreoElectronico;
            dto.Puerto = entidad.Puerto;
            dto.Contrasenia = entidad.Contrasenia;
            dto.ServidorSmtp = entidad.ServidorSmtp;
            dto.AsuntoCreacionUsuario = entidad.AsuntoCreacionUsuario;
            dto.CuerpoCreacionUsuario = entidad.CuerpoCreacionUsuario;
            dto.AsuntoRecuperacionClave = entidad.AsuntoRecuperacionClave;
            dto.CuerpoRecuperacionClave = entidad.CuerpoRecuperacionClave;
            dto.AsuntoNuevoPedidoVendedor = entidad.AsuntoNuevoPedidoVendedor;
            dto.CuerpoNuevoPedidoVendedor = entidad.CuerpoNuevoPedidoVendedor;
            dto.AsuntoPedidoEnviadoCliente = entidad.AsuntoPedidoEnviadoCliente;
            dto.CuerpoPedidoEnviadoCliente = entidad.CuerpoPedidoEnviadoCliente;
            dto.AsuntoVerificacionResultado = entidad.AsuntoVerificacionResultado;
            dto.CuerpoVerificacionResultado = entidad.CuerpoVerificacionResultado;

            return dto;
        }
    }
}
