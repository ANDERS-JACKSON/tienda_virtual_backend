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
    public class RetiroDB : IEntityTypeConfiguration<Retiro>
    {
        public void Configure(EntityTypeBuilder<Retiro> builder)
        {
            builder.ToTable("retiro", "xqm_pago");
            builder.HasKey(e => e.RetiroId);

            builder.Property(e => e.RetiroId)
                .HasColumnName("retiro_id");

            builder.Property(e => e.VendedorId)
                .HasColumnName("vendedor_id")
                .IsRequired();

            builder.Property(e => e.CuentaId)
                .HasColumnName("cuenta_id")
                .IsRequired();

            builder.Property(e => e.Monto)
                .HasColumnName("monto")
                .HasColumnType("numeric(10,2)");

            builder.Property(e => e.Estado)
                .HasConversion<short>()
                .HasColumnType("int2")
                .HasColumnName("estado")
                .IsRequired();

            builder.Property(e => e.ProcesadoPor)
                .HasColumnName("procesado_por");

            builder.Property(e => e.ReferenciaTransferencia)
                .HasColumnName("referencia_transferencia")
                .HasMaxLength(100);

            builder.Property(e => e.FechaSolicitud)
                .HasColumnName("fecha_solicitud");

            builder.Property(e => e.FechaCompletado)
                .HasColumnName("fecha_completado");

            builder.HasOne(e => e.Vendedor)
                .WithMany(v => v.Retiros)
                .HasForeignKey(e => e.VendedorId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_retiro_vendedor");

            builder.HasOne(e => e.Cuenta)
                .WithMany()
                .HasForeignKey(e => e.CuentaId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_retiro_cuenta");

            builder.HasOne(e => e.ProcesadoPorUsuario)
                .WithMany(u => u.RetirosProcesados)
                .HasForeignKey(e => e.ProcesadoPor)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_retiro_procesado");
        }
    }
}
