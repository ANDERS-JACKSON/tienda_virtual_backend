using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TiendaVirtual.Dominio.Modelo.ConfiguracionXqm;

namespace TiendaVirtual.Dominio.ConfiguracionDB.ConfiguracionXqm
{
    public class AvisoBannerDB : IEntityTypeConfiguration<AvisoBanner>
    {
        public void Configure(EntityTypeBuilder<AvisoBanner> builder)
        {
            builder.ToTable("aviso_banner", "xqm_configuracion");
            builder.HasKey(e => e.AvisoBannerId);

            builder.Property(e => e.AvisoBannerId)
                .HasColumnName("aviso_banner_id");

            builder.Property(e => e.Texto)
                .HasColumnName("texto")
                .HasMaxLength(300)
                .IsRequired();

            builder.Property(e => e.Activo)
                .HasColumnName("activo");

            builder.Property(e => e.Orden)
                .HasColumnName("orden");
        }
    }
}
