using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Dominio.Modelo.PagoXqm;

namespace TiendaVirtual.Dominio.ConfiguracionDB.PagoXqm
{
    public class TransaccionDB : IEntityTypeConfiguration<Transaccion>
    {
        public void Configure(EntityTypeBuilder<Transaccion> builder)
        {
            builder.ToTable("transaccion", "xqm_pago");
            builder.HasKey(e => e.TransaccionId);

            builder.Property(e => e.TransaccionId)
                .HasColumnName("transaccion_id");

            builder.Property(e => e.OrdenId)
                .HasColumnName("orden_id");

            builder.Property(e => e.SuscripcionId)
                .HasColumnName("suscripcion_id");

            builder.Property(e => e.UsuarioId)
                .HasColumnName("usuario_id")
                .IsRequired();

            builder.Property(e => e.Proveedor)
                .HasColumnName("proveedor")
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(e => e.TransaccionProveedorId)
                .HasColumnName("transaccion_proveedor_id")
                .HasMaxLength(200);

            builder.Property(e => e.Tipo)
                .HasConversion<short>().HasColumnType("int2")
                .HasColumnName("tipo").IsRequired();

            builder.Property(e => e.Monto)
                .HasColumnName("monto")
                .HasColumnType("numeric(10,2)");

            builder.Property(e => e.Estado)
                .HasConversion<short>()
                .HasColumnType("int2")
                .HasColumnName("estado")
                .IsRequired();

            builder.Property(e => e.MetodoPago)
                .HasColumnName("metodo_pago")
                .HasMaxLength(50);

            builder.Property(e => e.RespuestaProveedor)
                .HasColumnName("respuesta_proveedor")
                .HasColumnType("jsonb");

            builder.Property(e => e.Fecha)
                .HasColumnName("fecha");

            builder.HasIndex(e => e.OrdenId)
                .HasDatabaseName("idx_transaccion_orden");

            builder.HasIndex(e => e.Estado)
                .HasDatabaseName("idx_transaccion_estado");

            builder.HasOne(e => e.Orden)
                .WithMany(o => o.Transacciones)
                .HasForeignKey(e => e.OrdenId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_transaccion_orden");

            builder.HasOne(e => e.Suscripcion)
                .WithMany(s => s.Transacciones)
                .HasForeignKey(e => e.SuscripcionId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_transaccion_suscripcion");

            builder.HasOne(e => e.Usuario)
                .WithMany(u => u.Transacciones)
                .HasForeignKey(e => e.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_transaccion_usuario");
        }
    }
}
