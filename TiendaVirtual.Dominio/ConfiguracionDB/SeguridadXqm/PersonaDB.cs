using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Dominio.Modelo.SeguridadXqm;

namespace TiendaVirtual.Dominio.ConfiguracionDB.SeguridadXqm
{
    public class PersonaDB : IEntityTypeConfiguration<Persona>
    {
        public void Configure(EntityTypeBuilder<Persona> builder)
        {
            builder.ToTable("persona", "xqm_seguridad");
            builder.HasKey(e => e.PersonaId);

            builder.Property(e => e.PersonaId)
                .HasColumnName("persona_id");

            builder.Property(e => e.TipoDocumento)
                .HasConversion<short>()
                .HasColumnType("int2")
                .HasColumnName("tipo_documento")
                .IsRequired();

            builder.Property(e => e.NumeroDocumento)
                .HasColumnName("numero_documento")
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(e => e.ApellidoPaterno)
                .HasColumnName("apellido_paterno")
                .HasMaxLength(100);

            builder.Property(e => e.ApellidoMaterno)
                .HasColumnName("apellido_materno")
                .HasMaxLength(100);

            builder.Property(e => e.Nombres)
                .HasColumnName("nombres")
                .HasMaxLength(150)
                .IsRequired();

            builder.Property(e => e.Sexo)
                .HasConversion<short>()
                .HasColumnType("int2")
                .HasColumnName("sexo");

            builder.Property(e => e.FechaNacimiento)
                .HasColumnType("date")
                .HasColumnName("fecha_nacimiento");

            builder.Property(e => e.Telefono)
                .HasColumnName("telefono")
                .HasMaxLength(20);

            builder.Property(e => e.CorreoElectronico)
                .HasColumnName("correo_electronico")
                .HasColumnType("citext");

            builder.HasIndex(e => e.NumeroDocumento)
                .HasDatabaseName("idx_persona_numero_documento");

            builder.HasIndex(e => new { e.TipoDocumento, e.NumeroDocumento })
                .IsUnique().HasDatabaseName("uq_persona_tipo_numero");
        }
    }
}
