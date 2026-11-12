using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TiendaVirtual.Dominio.Modelo.CatalogoXqm;

namespace TiendaVirtual.Dominio.ConfiguracionDB.CatalogoXqm
{
    public class OpcionVarianteDB : IEntityTypeConfiguration<OpcionVariante>
    {
        public void Configure(EntityTypeBuilder<OpcionVariante> builder)
        {
            builder.ToTable("opcion_variante", "xqm_catalogo");
            builder.HasKey(e => e.OpcionVarianteId);

            builder.Property(e => e.OpcionVarianteId)
                .HasColumnName("opcion_variante_id");

            builder.Property(e => e.Nombre)
                .HasColumnName("nombre")
                .HasMaxLength(80)
                .IsRequired();

            builder.Property(e => e.Orden)
                .HasColumnName("orden");

            builder.Property(e => e.Activo)
                .HasColumnName("activo");
        }
    }
}
