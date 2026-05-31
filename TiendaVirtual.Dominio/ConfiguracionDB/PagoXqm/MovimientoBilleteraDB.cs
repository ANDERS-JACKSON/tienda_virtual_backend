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
    public class MovimientoBilleteraDB : IEntityTypeConfiguration<MovimientoBilletera>
    {
        public void Configure(EntityTypeBuilder<MovimientoBilletera> builder)
        {
            builder.ToTable("movimiento_billetera", "xqm_pago");
            builder.HasKey(e => e.MovimientoId);

            builder.Property(e => e.MovimientoId)
                .HasColumnName("movimiento_id");

            builder.Property(e => e.VendedorId)
                .HasColumnName("vendedor_id")
                .IsRequired();

            builder.Property(e => e.Tipo)
                .HasConversion<short>()
                .HasColumnType("int2")
                .HasColumnName("tipo")
                .IsRequired();

            builder.Property(e => e.Monto)
                .HasColumnName("monto")
                .HasColumnType("numeric(10,2)");

            builder.Property(e => e.SaldoResultante)
                .HasColumnName("saldo_resultante")
                .HasColumnType("numeric(12,2)");

            builder.Property(e => e.ReferenciaId)
                .HasColumnName("referencia_id");

            builder.Property(e => e.Descripcion)
                .HasColumnName("descripcion")
                .HasMaxLength(300);

            builder.Property(e => e.Fecha)
                .HasColumnName("fecha");

            builder.HasIndex(e => e.VendedorId).HasDatabaseName("idx_movimiento_vendedor");

            builder.HasOne(e => e.Vendedor)
                .WithMany(v => v.MovimientosBilletera)
                .HasForeignKey(e => e.VendedorId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_movimiento_vendedor");
        }
    }
}
