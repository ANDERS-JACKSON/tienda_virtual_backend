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
    public class EnvioDB : IEntityTypeConfiguration<Envio>
    {
        public void Configure(EntityTypeBuilder<Envio> builder)
        {
            builder.ToTable("envio", "xqm_venta");
            builder.HasKey(e => e.EnvioId);

            builder.Property(e => e.EnvioId)
                .HasColumnName("envio_id");

            builder.Property(e => e.SubordenId)
                .HasColumnName("suborden_id")
                .IsRequired();

            builder.Property(e => e.Transportista)
                .HasColumnName("transportista")
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(e => e.NumeroSeguimiento)
                .HasColumnName("numero_seguimiento")
                .HasMaxLength(100);

            builder.Property(e => e.ComprobanteUrl)
                .HasColumnName("comprobante_url")
                .HasMaxLength(500);

            builder.Property(e => e.MontoComprobante)
                .HasColumnName("monto_comprobante")
                .HasColumnType("numeric(10,2)");

            builder.Property(e => e.FechaEntregaReal)
                .HasColumnName("fecha_entrega_real")
                .HasColumnType("date");

            builder.HasIndex(e => e.SubordenId)
                .HasDatabaseName("idx_envio_suborden");

            builder.HasOne(e => e.Suborden)
                .WithMany(s => s.Envios)
                .HasForeignKey(e => e.SubordenId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_envio_suborden");
        }
    }
}
