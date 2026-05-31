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
    public class BilleteraDB : IEntityTypeConfiguration<Billetera>
    {
        public void Configure(EntityTypeBuilder<Billetera> builder)
        {
            builder.ToTable("billetera", "xqm_pago");
            builder.HasKey(e => e.VendedorId);

            builder.Property(e => e.VendedorId)
                .HasColumnName("vendedor_id");

            builder.Property(e => e.SaldoDisponible)
                .HasColumnName("saldo_disponible")
                .HasColumnType("numeric(12,2)");

            builder.Property(e => e.SaldoPendiente)
                .HasColumnName("saldo_pendiente")
                .HasColumnType("numeric(12,2)");

            builder.Property(e => e.TotalGanado)
                .HasColumnName("total_ganado")
                .HasColumnType("numeric(12,2)");

            builder.Property(e => e.TotalRetirado)
                .HasColumnName("total_retirado")
                .HasColumnType("numeric(12,2)");

            builder.HasOne(e => e.Vendedor)
                .WithOne(v => v.Billetera!)
                .HasForeignKey<Billetera>(e => e.VendedorId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_billetera_vendedor");
        }
    }
}
