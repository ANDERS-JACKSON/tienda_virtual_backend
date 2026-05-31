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
    public class NotificacionDB : IEntityTypeConfiguration<Notificacion>
    {
        public void Configure(EntityTypeBuilder<Notificacion> builder)
        {
            builder.ToTable("notificacion", "xqm_soporte");
            builder.HasKey(e => e.NotificacionId);

            builder.Property(e => e.NotificacionId)
                .HasColumnName("notificacion_id");

            builder.Property(e => e.UsuarioId)
                .HasColumnName("usuario_id")
                .IsRequired();

            builder.Property(e => e.Tipo)
                .HasColumnName("tipo")
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(e => e.Titulo)
                .HasColumnName("titulo")
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(e => e.Cuerpo)
                .HasColumnName("cuerpo")
                .HasMaxLength(1000);

            builder.Property(e => e.Datos)
                .HasColumnName("datos")
                .HasColumnType("jsonb");

            builder.Property(e => e.Leida)
                .HasColumnName("leida");
            builder.Property(e => e.Fecha)
                .HasColumnName("fecha");

            builder.HasIndex(e => new { e.UsuarioId, e.Leida })
                .HasDatabaseName("idx_notificacion_usuario_leida");

            builder.HasOne(e => e.Usuario)
                .WithMany(u => u.Notificaciones)
                .HasForeignKey(e => e.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_notificacion_usuario");
        }
    }
}
