using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TiendaVirtual.Dominio.Modelo.SeguridadXqm;

namespace TiendaVirtual.Dominio.ConfiguracionDB.SeguridadXqm
{
    public class CodigoAccesoCorreoDB : IEntityTypeConfiguration<CodigoAccesoCorreo>
    {
        public void Configure(EntityTypeBuilder<CodigoAccesoCorreo> builder)
        {
            builder.ToTable("codigo_acceso_correo", "xqm_seguridad");
            builder.HasKey(e => e.CodigoAccesoId);

            builder.Property(e => e.CodigoAccesoId).HasColumnName("codigo_acceso_id");

            builder.Property(e => e.Correo)
                .HasColumnName("correo")
                .HasColumnType("citext")
                .IsRequired();

            builder.Property(e => e.CodigoHash)
                .HasColumnName("codigo_hash")
                .HasMaxLength(128)
                .IsRequired();

            builder.Property(e => e.ExpiraEn)
                .HasColumnName("expira_en")
                .HasColumnType("timestamptz");

            builder.Property(e => e.Intentos)
                .HasColumnName("intentos")
                .IsRequired();

            builder.Property(e => e.Usado)
                .HasColumnName("usado")
                .IsRequired();

            builder.Property(e => e.FechaCreacion)
                .HasColumnName("fecha_creacion")
                .HasColumnType("timestamptz");

            builder.Property(e => e.DireccionIp)
                .HasColumnName("direccion_ip")
                .HasMaxLength(64);

            builder.HasIndex(e => new { e.Correo, e.FechaCreacion })
                .HasDatabaseName("idx_codigo_acceso_correo_lookup");

            builder.HasIndex(e => new { e.DireccionIp, e.FechaCreacion })
                .HasDatabaseName("idx_codigo_acceso_correo_ip");
        }
    }
}
