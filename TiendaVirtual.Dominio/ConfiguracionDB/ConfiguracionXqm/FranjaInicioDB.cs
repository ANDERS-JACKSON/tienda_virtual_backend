using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TiendaVirtual.Dominio.Modelo.ConfiguracionXqm;

namespace TiendaVirtual.Dominio.ConfiguracionDB.ConfiguracionXqm
{
    public class FranjaInicioDB : IEntityTypeConfiguration<FranjaInicio>
    {
        public void Configure(EntityTypeBuilder<FranjaInicio> builder)
        {
            builder.ToTable("franja_inicio", "xqm_configuracion");
            builder.HasKey(e => e.FranjaInicioId);

            builder.Property(e => e.FranjaInicioId).HasColumnName("franja_inicio_id");
            builder.Property(e => e.Zona).HasColumnName("zona").HasMaxLength(20).IsRequired();
            builder.Property(e => e.ImagenUrl).HasColumnName("imagen_url").HasMaxLength(500).IsRequired();
            builder.Property(e => e.Titulo).HasColumnName("titulo").HasMaxLength(80);
            builder.Property(e => e.EnlaceUrl).HasColumnName("enlace_url").HasMaxLength(500).IsRequired();
            builder.Property(e => e.Activo).HasColumnName("activo").HasDefaultValue(true);
            builder.Property(e => e.Orden).HasColumnName("orden").HasDefaultValue(0);

            builder.HasIndex(e => new { e.Zona, e.Activo, e.Orden })
                .HasDatabaseName("ix_franja_inicio_zona_activo_orden");
        }
    }
}
