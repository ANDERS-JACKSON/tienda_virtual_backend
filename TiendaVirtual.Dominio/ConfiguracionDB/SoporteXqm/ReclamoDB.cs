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
    public class ReclamoDB : IEntityTypeConfiguration<Reclamo>
    {
        public void Configure(EntityTypeBuilder<Reclamo> builder)
        {
            builder.ToTable("reclamo", "xqm_soporte");
            builder.HasKey(e => e.ReclamoId);

            builder.Property(e => e.ReclamoId)
                .HasColumnName("reclamo_id");

            builder.Property(e => e.SubordenId)
                .HasColumnName("suborden_id")
                .IsRequired();

            builder.Property(e => e.AbiertoPor)
                .HasColumnName("abierto_por")
                .IsRequired();

            builder.Property(e => e.Motivo)
                .HasConversion<short>()
                .HasColumnType("int2")
                .HasColumnName("motivo")
                .IsRequired();

            builder.Property(e => e.Descripcion)
                .HasColumnName("descripcion")
                .HasMaxLength(2000);

            builder.Property(e => e.Evidencias)
                .HasColumnName("evidencias")
                .HasColumnType("jsonb");

            builder.Property(e => e.Estado)
                .HasConversion<short>()
                .HasColumnType("int2")
                .HasColumnName("estado")
                .IsRequired();

            builder.Property(e => e.NotasResolucion)
                .HasColumnName("notas_resolucion")
                .HasMaxLength(2000);

            builder.Property(e => e.ResueltoPor)
                .HasColumnName("resuelto_por");

            builder.Property(e => e.MontoReembolso)
                .HasColumnName("monto_reembolso")
                .HasColumnType("numeric(10,2)");

            builder.Property(e => e.FechaApertura)
                .HasColumnName("fecha_apertura");

            builder.Property(e => e.FechaResolucion)
                .HasColumnName("fecha_resolucion");

            builder.HasIndex(e => e.Estado)
                .HasDatabaseName("idx_reclamo_estado");

            builder.HasIndex(e => e.SubordenId)
                .HasDatabaseName("idx_reclamo_suborden");

            builder.HasOne(e => e.Suborden)
                .WithMany(s => s.Reclamos)
                .HasForeignKey(e => e.SubordenId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_reclamo_suborden");

            builder.HasOne(e => e.AbiertoPorUsuario)
                .WithMany(u => u.ReclamosAbiertos)
                .HasForeignKey(e => e.AbiertoPor)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_reclamo_abierto");

            builder.HasOne(e => e.ResueltoPorUsuario)
                .WithMany(u => u.ReclamosResueltos)
                .HasForeignKey(e => e.ResueltoPor)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_reclamo_resuelto");
        }
    }
}
