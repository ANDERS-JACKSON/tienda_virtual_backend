using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Dominio.Modelo.VendedorXqm;

namespace TiendaVirtual.Dominio.ConfiguracionDB.VendedorXqm
{
    public class CuentaBancariaDB : IEntityTypeConfiguration<CuentaBancaria>
    {
        public void Configure(EntityTypeBuilder<CuentaBancaria> builder)
        {
            builder.ToTable("cuenta_bancaria", "xqm_vendedor");
            builder.HasKey(e => e.CuentaId);

            builder.Property(e => e.CuentaId)
                .HasColumnName("cuenta_id");

            builder.Property(e => e.VendedorId)
                .HasColumnName("vendedor_id")
                .IsRequired();

            builder.Property(e => e.Banco)
                .HasColumnName("banco")
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(e => e.NumeroCuenta)
                .HasColumnName("numero_cuenta")
                .HasMaxLength(30)
                .IsRequired();

            builder.Property(e => e.Cci)
                .HasColumnName("cci")
                .HasMaxLength(30);

            builder.Property(e => e.Titular)
                .HasColumnName("titular")
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(e => e.EsPredeterminada)
                .HasColumnName("es_predeterminada");

            builder.HasOne(e => e.Vendedor)
                .WithMany(v => v.CuentasBancarias)
                .HasForeignKey(e => e.VendedorId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_cuenta_vendedor");
        }
    }
}
