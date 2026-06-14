using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TiendaVirtual.Dominio.Modelo.CatalogoXqm;

namespace TiendaVirtual.Dominio.ConfiguracionDB.CatalogoXqm
{
    public class ProductoDestacadoDB : IEntityTypeConfiguration<ProductoDestacado>
    {
        public void Configure(EntityTypeBuilder<ProductoDestacado> builder)
        {
            builder.ToTable("producto_destacado", "xqm_catalogo");
            builder.HasKey(e => e.ProductoDestacadoId);

            builder.Property(e => e.ProductoDestacadoId)
                .HasColumnName("producto_destacado_id");

            builder.Property(e => e.ProductoId)
                .HasColumnName("producto_id")
                .IsRequired();

            builder.Property(e => e.Orden)
                .HasColumnName("orden")
                .HasDefaultValue(0)
                .IsRequired();

            builder.HasIndex(e => e.ProductoId)
                .IsUnique()
                .HasDatabaseName("uq_producto_destacado_producto");

            builder.HasOne(e => e.Producto)
                .WithOne(p => p.Destacado)
                .HasForeignKey<ProductoDestacado>(e => e.ProductoId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_producto_destacado_producto");
        }
    }
}
