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
    public class FavoritoDB : IEntityTypeConfiguration<Favorito>
    {
        public void Configure(EntityTypeBuilder<Favorito> builder)
        {
            builder.ToTable("favorito", "xqm_catalogo");
            builder.HasKey(e => new { e.UsuarioId, e.ProductoId });

            builder.Property(e => e.UsuarioId)
                .HasColumnName("usuario_id");

            builder.Property(e => e.ProductoId)
                .HasColumnName("producto_id");

            builder.Property(e => e.Fecha)
                .HasColumnName("fecha");

            builder.HasIndex(e => e.UsuarioId)
                .HasDatabaseName("idx_favorito_usuario");

            builder.HasOne(e => e.Usuario)
                .WithMany(u => u.Favoritos)
                .HasForeignKey(e => e.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_favorito_usuario");

            builder.HasOne(e => e.Producto)
                .WithMany(p => p.Favoritos)
                .HasForeignKey(e => e.ProductoId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_favorito_producto");
        }
    }
}
