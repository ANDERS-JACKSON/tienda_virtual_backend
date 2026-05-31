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
    public class ItemOrdenDB : IEntityTypeConfiguration<ItemOrden>
    {
        public void Configure(EntityTypeBuilder<ItemOrden> builder)
        {
            builder.ToTable("item_orden", "xqm_venta");
            builder.HasKey(e => e.ItemOrdenId);

            builder.Property(e => e.ItemOrdenId)
                .HasColumnName("item_orden_id");

            builder.Property(e => e.SubordenId)
                .HasColumnName("suborden_id")
                .IsRequired();

            builder.Property(e => e.VarianteId)
                .HasColumnName("variante_id");

            builder.Property(e => e.NombreProducto)
                .HasColumnName("nombre_producto")
                .HasMaxLength(250)
                .IsRequired();

            builder.Property(e => e.NombreVariante)
                .HasColumnName("nombre_variante")
                .HasMaxLength(200);

            builder.Property(e => e.PrecioUnitario)
                .HasColumnName("precio_unitario")
                .HasColumnType("numeric(10,2)");

            builder.Property(e => e.Cantidad)
                .HasColumnName("cantidad");

            builder.Property(e => e.TotalLinea)
                .HasColumnName("total_linea")
                .HasColumnType("numeric(10,2)");

            builder.Property(e => e.ImagenUrl)
                .HasColumnName("imagen_url")
                .HasMaxLength(500);

            builder.Property(e => e.TipoProducto)
                .HasConversion<short>()
                .HasColumnType("int2")
                .HasColumnName("tipo_producto")
                .IsRequired();

            builder.Property(e => e.ArchivoPatronUrl)
                .HasColumnName("archivo_patron_url")
                .HasMaxLength(500);

            builder.HasOne(e => e.Suborden)
                .WithMany(s => s.Items)
                .HasForeignKey(e => e.SubordenId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_item_orden_suborden");

            builder.HasOne(e => e.Variante)
                .WithMany(v => v.ItemsOrden)
                .HasForeignKey(e => e.VarianteId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_item_orden_variante");
        }
    }
}
