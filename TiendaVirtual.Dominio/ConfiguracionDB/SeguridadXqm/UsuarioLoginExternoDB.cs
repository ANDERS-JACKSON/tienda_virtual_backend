using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TiendaVirtual.Dominio.Modelo.SeguridadXqm;

namespace TiendaVirtual.Dominio.ConfiguracionDB.SeguridadXqm
{
    public class UsuarioLoginExternoDB : IEntityTypeConfiguration<UsuarioLoginExterno>
    {
        public void Configure(EntityTypeBuilder<UsuarioLoginExterno> builder)
        {
            builder.ToTable("usuario_login_externo", "xqm_seguridad");
            builder.HasKey(e => e.UsuarioLoginExternoId);

            builder.Property(e => e.UsuarioLoginExternoId)
                .HasColumnName("usuario_login_externo_id");

            builder.Property(e => e.UsuarioId)
                .HasColumnName("usuario_id")
                .IsRequired();

            builder.Property(e => e.Proveedor)
                .HasColumnName("proveedor")
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(e => e.SubjectId)
                .HasColumnName("subject_id")
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(e => e.FechaVinculacion)
                .HasColumnName("fecha_vinculacion");

            builder.HasIndex(e => new { e.Proveedor, e.SubjectId })
                .IsUnique()
                .HasDatabaseName("uq_login_externo_proveedor_subject");

            builder.HasIndex(e => new { e.UsuarioId, e.Proveedor })
                .IsUnique()
                .HasDatabaseName("uq_login_externo_usuario_proveedor");

            builder.HasOne(e => e.Usuario)
                .WithMany()
                .HasForeignKey(e => e.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_usuario_login_externo_usuario");
        }
    }
}
