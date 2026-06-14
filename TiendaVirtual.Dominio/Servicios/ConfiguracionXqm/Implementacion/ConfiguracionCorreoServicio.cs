using Microsoft.EntityFrameworkCore;
using TiendaVirtual.Dominio.Modelo.ConfiguracionXqm;
using TiendaVirtual.Dominio.Servicios.SoporteXqm;
using SoporteEmail = TiendaVirtual.Dominio.Servicios.SoporteXqm.IEmailServicio;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.ConfiguracionXqm;

namespace TiendaVirtual.Dominio.Servicios.ConfiguracionXqm.Implementacion
{
    public class ConfiguracionCorreoServicio : IConfiguracionCorreoServicio
    {
        private readonly TiendaVirtualDbContext _context;
        private readonly SoporteEmail _email;

        public ConfiguracionCorreoServicio(TiendaVirtualDbContext context, SoporteEmail email)
        {
            _context = context;
            _email = email;
        }

        public async Task<ResultadoOperacion<ConfiguracionCorreoDto>> ObtenerAsync()
        {
            try
            {
                var config = await _context.Correo.AsNoTracking().FirstOrDefaultAsync();
                return ResultadoOperacion<ConfiguracionCorreoDto>.SetExito(MapearDto(config));
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<ConfiguracionCorreoDto>.SetError("Error: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<ConfiguracionCorreoDto>> ActualizarSmtpAsync(ActualizarSmtpDto dto)
        {
            try
            {
                if (dto == null)
                    return ResultadoOperacion<ConfiguracionCorreoDto>.SetError("Datos inválidos.");
                if (string.IsNullOrWhiteSpace(dto.ServidorSmtp))
                    return ResultadoOperacion<ConfiguracionCorreoDto>.SetError("El servidor SMTP es obligatorio.");
                if (string.IsNullOrWhiteSpace(dto.CorreoElectronico))
                    return ResultadoOperacion<ConfiguracionCorreoDto>.SetError("El correo electrónico es obligatorio.");

                var config = await _context.Correo.FirstOrDefaultAsync();
                if (config == null)
                {
                    config = new Correo();
                    _context.Correo.Add(config);
                }

                config.ServidorSmtp = dto.ServidorSmtp.Trim();
                config.Puerto = dto.Puerto;
                config.CorreoElectronico = dto.CorreoElectronico.Trim().ToLower();
                config.Remitente = dto.Remitente?.Trim();
                if (!string.IsNullOrWhiteSpace(dto.Contrasenia))
                    config.Contrasenia = dto.Contrasenia;

                await _context.SaveChangesAsync();
                return ResultadoOperacion<ConfiguracionCorreoDto>.SetExito(MapearDto(config));
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<ConfiguracionCorreoDto>.SetError("Error: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<ConfiguracionCorreoDto>> ActualizarPlantillasAsync(ActualizarPlantillasDto dto)
        {
            try
            {
                if (dto == null)
                    return ResultadoOperacion<ConfiguracionCorreoDto>.SetError("Datos inválidos.");

                var config = await _context.Correo.FirstOrDefaultAsync();
                if (config == null)
                {
                    config = new Correo();
                    _context.Correo.Add(config);
                }

                if (dto.CreacionUsuario != null)
                {
                    config.AsuntoCreacionUsuario = dto.CreacionUsuario.Asunto;
                    config.CuerpoCreacionUsuario = dto.CreacionUsuario.Cuerpo;
                }
                if (dto.RecuperacionClave != null)
                {
                    config.AsuntoRecuperacionClave = dto.RecuperacionClave.Asunto;
                    config.CuerpoRecuperacionClave = dto.RecuperacionClave.Cuerpo;
                }
                if (dto.NuevoPedidoVendedor != null)
                {
                    config.AsuntoNuevoPedidoVendedor = dto.NuevoPedidoVendedor.Asunto;
                    config.CuerpoNuevoPedidoVendedor = dto.NuevoPedidoVendedor.Cuerpo;
                }
                if (dto.PedidoEnviadoCliente != null)
                {
                    config.AsuntoPedidoEnviadoCliente = dto.PedidoEnviadoCliente.Asunto;
                    config.CuerpoPedidoEnviadoCliente = dto.PedidoEnviadoCliente.Cuerpo;
                }
                if (dto.VerificacionResultado != null)
                {
                    config.AsuntoVerificacionResultado = dto.VerificacionResultado.Asunto;
                    config.CuerpoVerificacionResultado = dto.VerificacionResultado.Cuerpo;
                }
                if (dto.NuevoMensajeContacto != null)
                {
                    config.AsuntoNuevoMensajeContacto = dto.NuevoMensajeContacto.Asunto;
                    config.CuerpoNuevoMensajeContacto = dto.NuevoMensajeContacto.Cuerpo;
                }

                await _context.SaveChangesAsync();
                return ResultadoOperacion<ConfiguracionCorreoDto>.SetExito(MapearDto(config));
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<ConfiguracionCorreoDto>.SetError("Error: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<bool>> EnviarPruebaAsync(int adminUsuarioId)
        {
            try
            {
                var admin = await _context.Usuarios
                    .Include(u => u.Persona)
                    .FirstOrDefaultAsync(u => u.UsuarioId == adminUsuarioId);
                if (admin == null)
                    return ResultadoOperacion<bool>.SetError("Usuario no encontrado.");

                var nombre = admin.Persona != null
                    ? $"{admin.Persona.Nombres} {admin.Persona.ApellidoPaterno}".Trim()
                    : admin.Correo;

                var enviado = await _email.EnviarPruebaAsync(admin.Correo, nombre);
                return enviado
                    ? ResultadoOperacion<bool>.SetExito(true)
                    : ResultadoOperacion<bool>.SetError(
                        "No se pudo enviar el email de prueba. Revisa la configuración SMTP en los logs.");
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<bool>.SetError("Error: " + ex.Message);
            }
        }

        private static ConfiguracionCorreoDto MapearDto(Correo? c)
        {
            if (c == null)
                return new ConfiguracionCorreoDto { CorreoId = 0, ContraseniaConfigurada = false };

            return new ConfiguracionCorreoDto
            {
                CorreoId = c.CorreoId,
                Remitente = c.Remitente,
                CorreoElectronico = c.CorreoElectronico,
                Puerto = c.Puerto,
                ContraseniaConfigurada = !string.IsNullOrWhiteSpace(c.Contrasenia),
                ServidorSmtp = c.ServidorSmtp,
                CreacionUsuario = new PlantillaCorreoItem
                {
                    Asunto = c.AsuntoCreacionUsuario,
                    Cuerpo = c.CuerpoCreacionUsuario
                },
                RecuperacionClave = new PlantillaCorreoItem
                {
                    Asunto = c.AsuntoRecuperacionClave,
                    Cuerpo = c.CuerpoRecuperacionClave
                },
                NuevoPedidoVendedor = new PlantillaCorreoItem
                {
                    Asunto = c.AsuntoNuevoPedidoVendedor,
                    Cuerpo = c.CuerpoNuevoPedidoVendedor
                },
                PedidoEnviadoCliente = new PlantillaCorreoItem
                {
                    Asunto = c.AsuntoPedidoEnviadoCliente,
                    Cuerpo = c.CuerpoPedidoEnviadoCliente
                },
                VerificacionResultado = new PlantillaCorreoItem
                {
                    Asunto = c.AsuntoVerificacionResultado,
                    Cuerpo = c.CuerpoVerificacionResultado
                },
                NuevoMensajeContacto = new PlantillaCorreoItem
                {
                    Asunto = c.AsuntoNuevoMensajeContacto,
                    Cuerpo = c.CuerpoNuevoMensajeContacto
                }
            };
        }
    }
}
