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
    public class SolicitudVerificacionDB : IEntityTypeConfiguration<SolicitudVerificacion>
    {
        public void Configure(EntityTypeBuilder<SolicitudVerificacion> builder)
        {
            builder.ToTable("solicitud_verificacion", "xqm_vendedor");
            builder.HasKey(e => e.SolicitudId);

            builder.Property(e => e.SolicitudId)
                .HasColumnName("solicitud_id");

            builder.Property(e => e.VendedorId)
                .HasColumnName("vendedor_id")
                .IsRequired();

            builder.Property(e => e.Estado)
                .HasConversion<short>()
                .HasColumnType("int2")
                .HasColumnName("estado")
                .IsRequired();

            builder.Property(e => e.DocumentoFrenteUrl)
                .HasColumnName("documento_frente_url")
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(e => e.DocumentoReversoUrl)
                .HasColumnName("documento_reverso_url")
                .HasMaxLength(500);

            builder.Property(e => e.SelfieDocumentoUrl)
                .HasColumnName("selfie_documento_url")
                .HasMaxLength(500);

            builder.Property(e => e.FotosProductos)
                .HasColumnName("fotos_productos")
                .HasColumnType("jsonb");

            builder.Property(e => e.VerificadorId)
                .HasColumnName("verificador_id");

            builder.Property(e => e.NotasRevisor)
                .HasColumnName("notas_revisor")
                .HasMaxLength(1000);

            builder.Property(e => e.MotivoRechazo)
                .HasColumnName("motivo_rechazo")
                .HasMaxLength(500);

            builder.Property(e => e.FechaEnvio)
                .HasColumnName("fecha_envio");

            builder.Property(e => e.FechaRevision)
                .HasColumnName("fecha_revision");

            builder.HasIndex(e => e.Estado).HasDatabaseName("idx_solicitud_estado");

            builder.HasOne(e => e.Vendedor)
                .WithMany(v => v.SolicitudesVerificacion)
                .HasForeignKey(e => e.VendedorId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_solicitud_vendedor");

            builder.HasOne(e => e.Verificador)
                .WithMany(u => u.SolicitudesVerificacionRevisadas)
                .HasForeignKey(e => e.VerificadorId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_solicitud_verificador");
        }
    }
}
