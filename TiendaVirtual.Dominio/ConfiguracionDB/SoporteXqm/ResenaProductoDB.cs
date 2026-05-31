using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Dominio.Modelo.SoporteXqm;

namespace TiendaVirtual.Dominio.ConfiguracionDB.SoporteXqm
{
    public class ResenaProductoDB : IEntityTypeConfiguration<ResenaProducto>
    {
        public void Configure(EntityTypeBuilder<ResenaProducto> builder)
        {
            builder.ToTable("resena_producto", "xqm_soporte");
            builder.HasKey(e => e.ResenaId);

            builder.Property(e => e.ResenaId)
                .HasColumnName("resena_id");

            builder.Property(e => e.ProductoId)
                .HasColumnName("producto_id")
                .IsRequired();

            builder.Property(e => e.ItemOrdenId)
                .HasColumnName("item_orden_id")
                .IsRequired();

            builder.Property(e => e.ClienteId)
                .HasColumnName("cliente_id")
                .IsRequired();

            builder.Property(e => e.Calificacion)
                .HasColumnName("calificacion")
                .HasColumnType("int2");

            builder.Property(e => e.Titulo)
                .HasColumnName("titulo")
                .HasMaxLength(150);

            builder.Property(e => e.Comentario)
                .HasColumnName("comentario")
                .HasMaxLength(1000);

            builder.Property(e => e.Imagenes)
                .HasColumnName("imagenes")
                .HasColumnType("jsonb");

            builder.Property(e => e.RespuestaVendedor)
                .HasColumnName("respuesta_vendedor")
                .HasMaxLength(1000);

            builder.Property(e => e.Fecha)
                .HasColumnName("fecha");

            builder.HasIndex(e => e.ItemOrdenId).IsUnique().HasDatabaseName("uq_resena_item_orden");
            builder.HasIndex(e => e.ProductoId).HasDatabaseName("idx_resena_producto");

            builder.HasOne(e => e.Producto)
                .WithMany(p => p.Resenas)
                .HasForeignKey(e => e.ProductoId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_resena_producto");

            builder.HasOne(e => e.ItemOrden)
                .WithOne(i => i.Resena!)
                .HasForeignKey<ResenaProducto>(e => e.ItemOrdenId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_resena_item_orden");

            builder.HasOne(e => e.Cliente)
                .WithMany(u => u.ResenasProducto)
                .HasForeignKey(e => e.ClienteId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_resena_cliente");
        }
    }
}
