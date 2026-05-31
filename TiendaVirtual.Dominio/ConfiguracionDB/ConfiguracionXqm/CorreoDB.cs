using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Dominio.Modelo.ConfiguracionXqm;

namespace TiendaVirtual.Dominio.ConfiguracionDB.ConfiguracionXqm
{
    public class CorreoDB : IEntityTypeConfiguration<Correo>
    {
        public void Configure(EntityTypeBuilder<Correo> builder)
        {
            builder.ToTable("correo", "xqm_configuracion");
            builder.HasKey(e => e.CorreoId);

            builder.Property(e => e.CorreoId)
                .HasColumnName("correo_id");

            builder.Property(e => e.Remitente)
                .HasColumnName("remitente")
                .HasMaxLength(150);

            builder.Property(e => e.CorreoElectronico)
                .HasColumnName("correo_electronico")
                .HasMaxLength(150);

            builder.Property(e => e.Puerto)
                .HasColumnName("puerto")
                .HasColumnType("int2");

            builder.Property(e => e.Contrasenia)
                .HasColumnName("contrasenia")
                .HasMaxLength(255);

            builder.Property(e => e.ServidorSmtp)
                .HasColumnName("servidor_smtp")
                .HasMaxLength(150);

            builder.Property(e => e.AsuntoCreacionUsuario)
                .HasColumnName("asunto_creacion_usuario")
                .HasMaxLength(250);

            builder.Property(e => e.CuerpoCreacionUsuario)
                .HasColumnName("cuerpo_creacion_usuario")
                .HasColumnType("text");

            builder.Property(e => e.AsuntoRecuperacionClave)
                .HasColumnName("asunto_recuperacion_clave")
                .HasMaxLength(250);

            builder.Property(e => e.CuerpoRecuperacionClave)
                .HasColumnName("cuerpo_recuperacion_clave")
                .HasColumnType("text");

            builder.Property(e => e.AsuntoNuevoPedidoVendedor)
                .HasColumnName("asunto_nuevo_pedido_vendedor")
                .HasMaxLength(250);

            builder.Property(e => e.CuerpoNuevoPedidoVendedor)
                .HasColumnName("cuerpo_nuevo_pedido_vendedor")
                .HasColumnType("text");

            builder.Property(e => e.AsuntoPedidoEnviadoCliente)
                .HasColumnName("asunto_pedido_enviado_cliente")
                .HasMaxLength(250);

            builder.Property(e => e.CuerpoPedidoEnviadoCliente)
                .HasColumnName("cuerpo_pedido_enviado_cliente")
                .HasColumnType("text");

            builder.Property(e => e.AsuntoVerificacionResultado)
                .HasColumnName("asunto_verificacion_resultado")
                .HasMaxLength(250);

            builder.Property(e => e.CuerpoVerificacionResultado)
                .HasColumnName("cuerpo_verificacion_resultado")
                .HasColumnType("text");
        }
    }
}
