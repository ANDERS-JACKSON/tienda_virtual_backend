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
    public class VarianteProductoDB : IEntityTypeConfiguration<VarianteProducto>
    {
        public void Configure(EntityTypeBuilder<VarianteProducto> builder)
        {
            builder.ToTable("variante_producto", "xqm_catalogo");
            builder.HasKey(e => e.VarianteId);

            builder.Property(e => e.VarianteId)
                .HasColumnName("variante_id");

            builder.Property(e => e.ProductoId)
                .HasColumnName("producto_id")
                .IsRequired();

            builder.Property(e => e.Sku)
                .HasColumnName("sku")
                .HasMaxLength(100);

            builder.Property(e => e.Nombre)
                .HasColumnName("nombre")
                .HasMaxLength(200);

            builder.Property(e => e.Precio)
                .HasColumnName("precio")
                .HasColumnType("numeric(10,2)");

            builder.Property(e => e.PesoGramos)
                .HasColumnName("peso_gramos");

            builder.Property(e => e.Atributos)
                .HasColumnName("atributos")
                .HasColumnType("jsonb");

            builder.Property(e => e.Activa)
                .HasColumnName("activa");

            builder.Property(e => e.OpcionVarianteId)
                .HasColumnName("opcion_variante_id");

            builder.Property(e => e.Material)
                .HasColumnName("material")
                .HasMaxLength(200);

            builder.Property(e => e.Dimensiones)
                .HasColumnName("dimensiones")
                .HasMaxLength(200);

            builder.Property(e => e.DescripcionCorta)
                .HasColumnName("descripcion_corta")
                .HasMaxLength(150);

            builder.HasIndex(e => e.ProductoId).HasDatabaseName("idx_variante_producto");

            builder.HasOne(e => e.Producto)
                .WithMany(p => p.Variantes)
                .HasForeignKey(e => e.ProductoId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_variante_producto");

            builder.HasOne(e => e.OpcionVariante)
                .WithMany(o => o.Variantes)
                .HasForeignKey(e => e.OpcionVarianteId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_variante_producto_opcion");
        }
    }
}
