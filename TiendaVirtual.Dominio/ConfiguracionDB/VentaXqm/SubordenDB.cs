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
    public class SubordenDB : IEntityTypeConfiguration<Suborden>
    {
        public void Configure(EntityTypeBuilder<Suborden> builder)
        {
            builder.ToTable("suborden", "xqm_venta");
            builder.HasKey(e => e.SubordenId);

            builder.Property(e => e.SubordenId)
                .HasColumnName("suborden_id");

            builder.Property(e => e.OrdenId)
                .HasColumnName("orden_id")
                .IsRequired();

            builder.Property(e => e.VendedorId)
                .HasColumnName("vendedor_id")
                .IsRequired();

            builder.Property(e => e.NumeroSuborden)
                .HasColumnName("numero_suborden")
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(e => e.Subtotal)
                .HasColumnName("subtotal")
                .HasColumnType("numeric(10,2)");

            builder.Property(e => e.MontoEnvio)
                .HasColumnName("monto_envio")
                .HasColumnType("numeric(10,2)");

            builder.Property(e => e.MontoComision)
                .HasColumnName("monto_comision")
                .HasColumnType("numeric(10,2)");

            builder.Property(e => e.MontoVendedor)
                .HasColumnName("monto_vendedor")
                .HasColumnType("numeric(10,2)");

            builder.Property(e => e.Estado)
                .HasConversion<short>()
                .HasColumnType("int2")
                .HasColumnName("estado")
                .IsRequired();

            builder.Property(e => e.FechaEnvio)
                .HasColumnName("fecha_envio");

            builder.Property(e => e.FechaEntrega)
                .HasColumnName("fecha_entrega");

            builder.HasIndex(e => e.NumeroSuborden).IsUnique().HasDatabaseName("uq_suborden_numero");
            builder.HasIndex(e => new { e.VendedorId, e.Estado }).HasDatabaseName("idx_suborden_vendedor");
            builder.HasIndex(e => e.OrdenId).HasDatabaseName("idx_suborden_orden");

            builder.HasOne(e => e.Orden)
                .WithMany(o => o.Subordenes)
                .HasForeignKey(e => e.OrdenId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_suborden_orden");

            builder.HasOne(e => e.Vendedor)
                .WithMany(v => v.Subordenes)
                .HasForeignKey(e => e.VendedorId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_suborden_vendedor");
        }
    }
}
