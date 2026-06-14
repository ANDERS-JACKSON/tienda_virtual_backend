using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TiendaVirtual.Dominio.Modelo.SoporteXqm;

namespace TiendaVirtual.Dominio.ConfiguracionDB.SoporteXqm
{
    public class MensajeContactoDB : IEntityTypeConfiguration<MensajeContacto>
    {
        public void Configure(EntityTypeBuilder<MensajeContacto> builder)
        {
            builder.ToTable("mensaje_contacto", "xqm_soporte");
            builder.HasKey(e => e.MensajeContactoId);

            builder.Property(e => e.MensajeContactoId)
                .HasColumnName("mensaje_contacto_id");

            builder.Property(e => e.UsuarioId)
                .HasColumnName("usuario_id");

            builder.Property(e => e.Nombre)
                .HasColumnName("nombre")
                .HasMaxLength(150)
                .IsRequired();

            builder.Property(e => e.Correo)
                .HasColumnName("correo")
                .HasMaxLength(150)
                .IsRequired();

            builder.Property(e => e.Asunto)
                .HasColumnName("asunto")
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(e => e.Mensaje)
                .HasColumnName("mensaje")
                .HasMaxLength(1000)
                .IsRequired();

            builder.Property(e => e.Estado)
                .HasConversion<short>()
                .HasColumnType("int2")
                .HasColumnName("estado")
                .IsRequired();

            builder.Property(e => e.RespondidoPor)
                .HasColumnName("respondido_por");

            builder.Property(e => e.Respuesta)
                .HasColumnName("respuesta")
                .HasMaxLength(1000);

            builder.Property(e => e.FechaRespuesta)
                .HasColumnName("fecha_respuesta");

            builder.Property(e => e.FechaMensaje)
                .HasColumnName("fecha_mensaje")
                .IsRequired();

            builder.HasIndex(e => e.Estado)
                .HasDatabaseName("idx_mensaje_contacto_estado");

            builder.HasIndex(e => e.FechaMensaje)
                .HasDatabaseName("idx_mensaje_contacto_fecha");

            builder.HasOne(e => e.Usuario)
                .WithMany()
                .HasForeignKey(e => e.UsuarioId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_mensaje_contacto_usuario");

            builder.HasOne(e => e.RespondidoPorUsuario)
                .WithMany()
                .HasForeignKey(e => e.RespondidoPor)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_mensaje_contacto_respondido_por");
        }
    }
}
