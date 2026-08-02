using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TiendaVirtual.Dominio.Modelo.SeguridadXqm;

namespace TiendaVirtual.Dominio.ConfiguracionDB.SeguridadXqm
{
    public class ProvinciaDB : IEntityTypeConfiguration<Provincia>
    {
        public void Configure(EntityTypeBuilder<Provincia> builder)
        {
            builder.ToTable("provincia", "xqm_seguridad");
            builder.HasKey(e => e.ProvinciaId);

            builder.Property(e => e.ProvinciaId)
                .HasColumnName("provincia_id")
                .HasMaxLength(4)
                .IsRequired();

            builder.Property(e => e.DepartamentoId)
                .HasColumnName("departamento_id")
                .HasMaxLength(2)
                .IsRequired();

            builder.Property(e => e.Nombre)
                .HasColumnName("nombre")
                .HasMaxLength(100)
                .IsRequired();

            builder.HasIndex(e => e.DepartamentoId).HasDatabaseName("idx_provincia_departamento");

            builder.HasOne(e => e.Departamento)
                .WithMany(d => d.Provincias)
                .HasForeignKey(e => e.DepartamentoId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_provincia_departamento");
        }
    }
}
