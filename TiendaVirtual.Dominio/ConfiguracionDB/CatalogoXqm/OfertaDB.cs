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
    public class OfertaDB : IEntityTypeConfiguration<Oferta>
    {
        public void Configure(EntityTypeBuilder<Oferta> builder)
        {
            builder.ToTable("oferta", "xqm_catalogo");
            builder.HasKey(e => e.OfertaId);

            builder.Property(e => e.OfertaId)
                .HasColumnName("oferta_id");

            builder.Property(e => e.ProductoId)
                .HasColumnName("producto_id")
                .IsRequired();

            builder.Property(e => e.Nombre)
                .HasColumnName("nombre")
                .HasMaxLength(100);

            builder.Property(e => e.PorcentajeDescuento)
                .HasColumnName("porcentaje_descuento")
                .HasColumnType("numeric(5,2)");

            builder.Property(e => e.PrecioOferta)
                .HasColumnName("precio_oferta")
                .HasColumnType("numeric(10,2)");

            builder.Property(e => e.FechaInicio)
                .HasColumnName("fecha_inicio");

            builder.Property(e => e.FechaFin)
                .HasColumnName("fecha_fin");

            builder.Property(e => e.Activa)
                .HasColumnName("activa");

            builder.HasIndex(e => e.ProductoId).HasDatabaseName("idx_oferta_producto");
            builder.HasIndex(e => new { e.ProductoId, e.Activa, e.FechaInicio, e.FechaFin })
                .HasDatabaseName("idx_oferta_activa_vigente");

            builder.HasOne(e => e.Producto)
                .WithMany(p => p.Ofertas)
                .HasForeignKey(e => e.ProductoId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_oferta_producto");
        }
    }
}
