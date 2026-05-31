using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Dominio.Modelo.SeguridadXqm;

namespace TiendaVirtual.Dominio.ConfiguracionDB.SeguridadXqm
{
    public class UsuarioDB : IEntityTypeConfiguration<Usuario>
    {
        public void Configure(EntityTypeBuilder<Usuario> builder)
        {
            builder.ToTable("usuario", "xqm_seguridad");
            builder.HasKey(e => e.UsuarioId);

            builder.Property(e => e.UsuarioId)
                .HasColumnName("usuario_id");

            builder.Property(e => e.PersonaId)
                .HasColumnName("persona_id")
                .IsRequired();

            builder.Property(e => e.Correo)
                .HasColumnName("correo")
                .HasColumnType("citext")
                .IsRequired();

            builder.Property(e => e.Contrasena)
                .HasColumnName("contrasena")
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(e => e.CorreoConfirmado)
                .HasColumnName("correo_confirmado");

            builder.Property(e => e.ForzarCambioClave)
                .HasColumnName("forzar_cambio_clave");

            builder.Property(e => e.Estado)
                .HasConversion<short>()
                .HasColumnType("int2")
                .HasColumnName("estado")
                .IsRequired();

            builder.Property(e => e.FechaAlta)
                .HasColumnName("fecha_alta");

            builder.Property(e => e.UltimoAcceso)
                .HasColumnName("ultimo_acceso");

            builder.Property(e => e.TwoFactorSecret)
                .HasColumnName("two_factor_secret").HasMaxLength(500);

            builder.Property(e => e.TwoFactorEnabled)
                .HasColumnName("two_factor_enabled");

            builder.HasIndex(e => e.Correo).IsUnique().HasDatabaseName("uq_usuario_correo");
            builder.HasIndex(e => e.PersonaId).HasDatabaseName("idx_usuario_persona");

            builder.HasOne(e => e.Persona)
                .WithMany(p => p.Usuarios)
                .HasForeignKey(e => e.PersonaId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_usuario_persona");
        }
    }
}
