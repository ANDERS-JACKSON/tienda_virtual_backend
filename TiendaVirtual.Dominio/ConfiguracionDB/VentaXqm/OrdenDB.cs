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
    public class OrdenDB : IEntityTypeConfiguration<Orden>
    {
        public void Configure(EntityTypeBuilder<Orden> builder)
        {
            builder.ToTable("orden", "xqm_venta");
            builder.HasKey(e => e.OrdenId);

            builder.Property(e => e.OrdenId)
                .HasColumnName("orden_id");

            builder.Property(e => e.NumeroOrden)
                .HasColumnName("numero_orden")
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(e => e.ClienteId)
                .HasColumnName("cliente_id")
                .IsRequired();

            builder.Property(e => e.Subtotal)
                .HasColumnName("subtotal")
                .HasColumnType("numeric(10,2)");

            builder.Property(e => e.TotalEnvio)
                .HasColumnName("total_envio")
                .HasColumnType("numeric(10,2)");

            builder.Property(e => e.TotalDescuento)
                .HasColumnName("total_descuento")
                .HasColumnType("numeric(10,2)");

            builder.Property(e => e.Total)
                .HasColumnName("total")
                .HasColumnType("numeric(10,2)");

            builder.Property(e => e.CorreoCliente)
                .HasColumnName("correo_cliente")
                .HasColumnType("citext")
                .IsRequired();

            builder.Property(e => e.TelefonoCliente)
                .HasColumnName("telefono_cliente")
                .HasMaxLength(20);

            builder.Property(e => e.DireccionEnvio)
                .HasColumnName("direccion_envio")
                .HasColumnType("jsonb")
                .IsRequired();

            builder.Property(e => e.Estado)
                .HasConversion<short>()
                .HasColumnType("int2")
                .HasColumnName("estado")
                .IsRequired();

            builder.Property(e => e.Fecha)
                .HasColumnName("fecha");

            builder.HasIndex(e => e.NumeroOrden)
                .IsUnique()
                .HasDatabaseName("uq_orden_numero");

            builder.HasIndex(e => e.ClienteId)
                .HasDatabaseName("idx_orden_cliente");

            builder.HasOne(e => e.Cliente)
                .WithMany(u => u.OrdenesComoCliente)
                .HasForeignKey(e => e.ClienteId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_orden_cliente");
        }
    }
}
