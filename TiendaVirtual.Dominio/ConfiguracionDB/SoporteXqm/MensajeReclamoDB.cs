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
    public class MensajeReclamoDB : IEntityTypeConfiguration<MensajeReclamo>
    {
        public void Configure(EntityTypeBuilder<MensajeReclamo> builder)
        {
            builder.ToTable("mensaje_reclamo", "xqm_soporte");
            builder.HasKey(e => e.MensajeId);

            builder.Property(e => e.MensajeId)
                .HasColumnName("mensaje_id");

            builder.Property(e => e.ReclamoId)
                .HasColumnName("reclamo_id")
                .IsRequired();

            builder.Property(e => e.RemitenteId)
                .HasColumnName("remitente_id")
                .IsRequired();

            builder.Property(e => e.Mensaje)
                .HasColumnName("mensaje")
                .HasMaxLength(2000)
                .IsRequired();

            builder.Property(e => e.Adjuntos)
                .HasColumnName("adjuntos")
                .HasColumnType("jsonb");

            builder.Property(e => e.Fecha)
                .HasColumnName("fecha");

            builder.HasOne(e => e.Reclamo)
                .WithMany(r => r.Mensajes)
                .HasForeignKey(e => e.ReclamoId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_mensaje_reclamo");

            builder.HasOne(e => e.Remitente)
                .WithMany(u => u.MensajesReclamo)
                .HasForeignKey(e => e.RemitenteId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_mensaje_remitente");
        }
    }
}
