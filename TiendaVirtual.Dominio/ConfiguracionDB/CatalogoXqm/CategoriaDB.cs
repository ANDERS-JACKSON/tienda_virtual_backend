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
    public class CategoriaDB : IEntityTypeConfiguration<Categoria>
    {
        public void Configure(EntityTypeBuilder<Categoria> builder)
        {
            builder.ToTable("categoria", "xqm_catalogo");
            builder.HasKey(e => e.CategoriaId);

            builder.Property(e => e.CategoriaId)
                .HasColumnName("categoria_id");

            builder.Property(e => e.CategoriaPadreId)
                .HasColumnName("categoria_padre_id");

            builder.Property(e => e.Nombre)
                .HasColumnName("nombre")
                .HasMaxLength(150)
                .IsRequired();

            builder.Property(e => e.Slug)
                .HasColumnName("slug")
                .HasMaxLength(150)
                .IsRequired();

            builder.Property(e => e.Descripcion)
                .HasColumnName("descripcion")
                .HasMaxLength(500);

            builder.Property(e => e.ImagenUrl)
                .HasColumnName("imagen_url")
                .HasMaxLength(500);

            builder.Property(e => e.Orden)
                .HasColumnName("orden");

            builder.Property(e => e.Activa)
                .HasColumnName("activa");

            builder.HasIndex(e => e.Slug).IsUnique().HasDatabaseName("uq_categoria_slug");

            builder.HasOne(e => e.CategoriaPadre)
                .WithMany(c => c.Subcategorias)
                .HasForeignKey(e => e.CategoriaPadreId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_categoria_padre");
        }
    }
}
