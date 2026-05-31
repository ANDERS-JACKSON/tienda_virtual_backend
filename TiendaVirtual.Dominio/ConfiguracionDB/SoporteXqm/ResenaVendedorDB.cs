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
    public class ResenaVendedorDB : IEntityTypeConfiguration<ResenaVendedor>
    {
        public void Configure(EntityTypeBuilder<ResenaVendedor> builder)
        {
            builder.ToTable("resena_vendedor", "xqm_soporte");
            builder.HasKey(e => e.ResenaId);

            builder.Property(e => e.ResenaId)
                .HasColumnName("resena_id");

            builder.Property(e => e.VendedorId)
                .HasColumnName("vendedor_id")
                .IsRequired();

            builder.Property(e => e.SubordenId)
                .HasColumnName("suborden_id")
                .IsRequired();

            builder.Property(e => e.ClienteId)
                .HasColumnName("cliente_id")
                .IsRequired();

            builder.Property(e => e.Calificacion)
                .HasColumnName("calificacion")
                .HasColumnType("int2");

            builder.Property(e => e.Comentario)
                .HasColumnName("comentario")
                .HasMaxLength(1000);

            builder.Property(e => e.Fecha)
                .HasColumnName("fecha");

            builder.HasIndex(e => e.SubordenId)
                .IsUnique().HasDatabaseName("uq_resena_vendedor_suborden");

            builder.HasIndex(e => e.VendedorId)
                .HasDatabaseName("idx_resena_vendedor_vendedor");

            builder.HasOne(e => e.Vendedor)
                .WithMany(v => v.Resenas)
                .HasForeignKey(e => e.VendedorId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_resena_vendedor_vendedor");

            builder.HasOne(e => e.Suborden)
                .WithOne(s => s.ResenaVendedor!)
                .HasForeignKey<ResenaVendedor>(e => e.SubordenId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_resena_vendedor_suborden");

            builder.HasOne(e => e.Cliente)
                .WithMany(u => u.ResenasVendedor)
                .HasForeignKey(e => e.ClienteId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_resena_vendedor_cliente");
        }
    }
}
