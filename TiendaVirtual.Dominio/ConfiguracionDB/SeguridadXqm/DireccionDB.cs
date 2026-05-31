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
    public class DireccionDB : IEntityTypeConfiguration<Direccion>
    {
        public void Configure(EntityTypeBuilder<Direccion> builder)
        {
            builder.ToTable("direccion", "xqm_seguridad");
            builder.HasKey(e => e.DireccionId);

            builder.Property(e => e.DireccionId)
                .HasColumnName("direccion_id");

            builder.Property(e => e.PersonaId)
                .HasColumnName("persona_id")
                .IsRequired();

            builder.Property(e => e.Etiqueta)
                .HasColumnName("etiqueta")
                .HasMaxLength(50);

            builder.Property(e => e.NombreReceptor)
                .HasColumnName("nombre_receptor")
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(e => e.Telefono)
                .HasColumnName("telefono")
                .HasMaxLength(20);

            builder.Property(e => e.Departamento)
                .HasColumnName("departamento")
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(e => e.Provincia)
                .HasColumnName("provincia")
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(e => e.Distrito)
                .HasColumnName("distrito")
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(e => e.DireccionLinea)
                .HasColumnName("direccion")
                .HasMaxLength(300)
                .IsRequired();

            builder.Property(e => e.Referencia)
                .HasColumnName("referencia")
                .HasMaxLength(300);

            builder.Property(e => e.EsPredeterminada)
                .HasColumnName("es_predeterminada");

            builder.HasIndex(e => e.PersonaId).HasDatabaseName("idx_direccion_persona");

            builder.HasOne(e => e.Persona)
                .WithMany(p => p.Direcciones)
                .HasForeignKey(e => e.PersonaId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_direccion_persona");
        }
    }
}
