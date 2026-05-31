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
    public class ProductoDB : IEntityTypeConfiguration<Producto>
    {
        public void Configure(EntityTypeBuilder<Producto> builder)
        {
            builder.ToTable("producto", "xqm_catalogo");
            builder.HasKey(e => e.ProductoId);

            builder.Property(e => e.ProductoId)
                .HasColumnName("producto_id");

            builder.Property(e => e.VendedorId)
                .HasColumnName("vendedor_id")
                .IsRequired();

            builder.Property(e => e.CategoriaId)
                .HasColumnName("categoria_id")
                .IsRequired();

            builder.Property(e => e.Nombre)
                .HasColumnName("nombre")
                .HasMaxLength(250)
                .IsRequired();

            builder.Property(e => e.Slug)
                .HasColumnName("slug")
                .HasMaxLength(250)
                .IsRequired();

            builder.Property(e => e.Descripcion)
                .HasColumnName("descripcion")
                .HasColumnType("text");

            builder.Property(e => e.DescripcionCorta)
                .HasColumnName("descripcion_corta")
                .HasMaxLength(300);

            builder.Property(e => e.Material)
                .HasColumnName("material")
                .HasMaxLength(200);

            builder.Property(e => e.Dimensiones)
                .HasColumnName("dimensiones")
                .HasMaxLength(200);

            builder.Property(e => e.TieneVariantes)
                .HasColumnName("tiene_variantes");

            builder.Property(e => e.PrecioBase)
                .HasColumnName("precio_base")
                .HasColumnType("numeric(10,2)");

            builder.Property(e => e.DiasElaboracion)
                .HasColumnName("dias_elaboracion");

            builder.Property(e => e.Estado)
                .HasConversion<short>().HasColumnType("int2")
                .HasColumnName("estado").IsRequired();

            builder.Property(e => e.Tipo)
                .HasConversion<short>().HasColumnType("int2")
                .HasColumnName("tipo").IsRequired();

            builder.Property(e => e.ArchivoPatronUrl)
                .HasColumnName("archivo_patron_url")
                .HasMaxLength(500);

            builder.Property(e => e.Vistas)
                .HasColumnName("vistas");

            builder.Property(e => e.Ventas)
                .HasColumnName("ventas");

            builder.Property(e => e.CalificacionPromedio)
                .HasColumnName("calificacion_promedio")
                .HasColumnType("numeric(3,2)");

            builder.Property(e => e.TotalResenas)
                .HasColumnName("total_resenas");

            builder.HasIndex(e => e.Slug).IsUnique().HasDatabaseName("uq_producto_slug");
            builder.HasIndex(e => new { e.VendedorId, e.Estado }).HasDatabaseName("idx_producto_vendedor_estado");
            builder.HasIndex(e => e.CategoriaId).HasDatabaseName("idx_producto_categoria");

            builder.HasOne(e => e.Vendedor)
                .WithMany(v => v.Productos)
                .HasForeignKey(e => e.VendedorId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_producto_vendedor");

            builder.HasOne(e => e.Categoria)
                .WithMany(c => c.Productos)
                .HasForeignKey(e => e.CategoriaId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_producto_categoria");
        }
    }
}
