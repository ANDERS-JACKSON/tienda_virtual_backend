using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Dominio.Modelo.CatalogoXqm;

namespace TiendaVirtual.Dominio.ConfiguracionDB.CatalogoXqm
{
    public class ImagenProductoDB : IEntityTypeConfiguration<ImagenProducto>
    {
        public void Configure(EntityTypeBuilder<ImagenProducto> builder)
        {
            builder.ToTable("imagen_producto", "xqm_catalogo");
            builder.HasKey(e => e.ImagenId);

            builder.Property(e => e.ImagenId)
                .HasColumnName("imagen_id");

            builder.Property(e => e.ProductoId)
                .HasColumnName("producto_id")
                .IsRequired();

            builder.Property(e => e.Url)
                .HasColumnName("url")
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(e => e.TextoAlt)
                .HasColumnName("texto_alt")
                .HasMaxLength(250);

            builder.Property(e => e.Orden)
                .HasColumnName("orden");

            builder.Property(e => e.EsPrincipal)
                .HasColumnName("es_principal");

            builder.HasOne(e => e.Producto)
                .WithMany(p => p.Imagenes)
                .HasForeignKey(e => e.ProductoId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_imagen_producto");
        }
    }
}
