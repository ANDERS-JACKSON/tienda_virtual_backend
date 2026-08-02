using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TiendaVirtual.Dominio.Modelo.SeguridadXqm;

namespace TiendaVirtual.Dominio.ConfiguracionDB.SeguridadXqm
{
    public class DistritoDB : IEntityTypeConfiguration<Distrito>
    {
        public void Configure(EntityTypeBuilder<Distrito> builder)
        {
            builder.ToTable("distrito", "xqm_seguridad");
            builder.HasKey(e => e.DistritoId);

            builder.Property(e => e.DistritoId)
                .HasColumnName("distrito_id")
                .HasMaxLength(6)
                .IsRequired();

            builder.Property(e => e.ProvinciaId)
                .HasColumnName("provincia_id")
                .HasMaxLength(4)
                .IsRequired();

            builder.Property(e => e.Nombre)
                .HasColumnName("nombre")
                .HasMaxLength(100)
                .IsRequired();

            builder.HasIndex(e => e.ProvinciaId).HasDatabaseName("idx_distrito_provincia");

            builder.HasOne(e => e.Provincia)
                .WithMany(p => p.Distritos)
                .HasForeignKey(e => e.ProvinciaId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_distrito_provincia");
        }
    }
}
