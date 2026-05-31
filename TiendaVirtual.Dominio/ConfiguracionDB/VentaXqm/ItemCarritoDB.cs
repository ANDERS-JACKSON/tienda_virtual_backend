using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Dominio.Modelo.VentaXqm;

namespace TiendaVirtual.Dominio.ConfiguracionDB.VentaXqm
{
    public class ItemCarritoDB : IEntityTypeConfiguration<ItemCarrito>
    {
        public void Configure(EntityTypeBuilder<ItemCarrito> builder)
        {
            builder.ToTable("item_carrito", "xqm_venta");
            builder.HasKey(e => e.ItemCarritoId);

            builder.Property(e => e.ItemCarritoId)
                .HasColumnName("item_carrito_id");

            builder.Property(e => e.CarritoId)
                .HasColumnName("carrito_id")
                .IsRequired();

            builder.Property(e => e.VarianteId)
                .HasColumnName("variante_id")
                .IsRequired();

            builder.Property(e => e.Cantidad)
                .HasColumnName("cantidad");

            builder.HasOne(e => e.Carrito)
                .WithMany(c => c.Items)
                .HasForeignKey(e => e.CarritoId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_item_carrito_carrito");

            builder.HasOne(e => e.Variante)
                .WithMany(v => v.ItemsCarrito)
                .HasForeignKey(e => e.VarianteId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_item_carrito_variante");
        }
    }
}
