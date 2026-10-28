using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TiendaVirtual.Dominio.Modelo.SeguridadXqm;

namespace TiendaVirtual.Dominio.ConfiguracionDB.SeguridadXqm
{
    public class EventoAuditoriaDB : IEntityTypeConfiguration<EventoAuditoria>
    {
        public void Configure(EntityTypeBuilder<EventoAuditoria> builder)
        {
            builder.ToTable("evento_auditoria", "xqm_seguridad");
            builder.HasKey(e => e.EventoId);

            builder.Property(e => e.EventoId)
                .HasColumnName("evento_id")
                .ValueGeneratedOnAdd();

            builder.Property(e => e.Fecha)
                .HasColumnName("fecha")
                .HasColumnType("timestamptz")
                .IsRequired();

            builder.Property(e => e.Accion)
                .HasColumnName("accion")
                .HasMaxLength(80)
                .IsRequired();

            builder.Property(e => e.UsuarioId)
                .HasColumnName("usuario_id");

            builder.Property(e => e.DireccionIp)
                .HasColumnName("direccion_ip")
                .HasMaxLength(45);

            builder.Property(e => e.Detalle)
                .HasColumnName("detalle")
                .HasMaxLength(500);

            builder.HasIndex(e => e.Fecha).HasDatabaseName("idx_evento_auditoria_fecha");
            builder.HasIndex(e => e.UsuarioId).HasDatabaseName("idx_evento_auditoria_usuario");
        }
    }
}
