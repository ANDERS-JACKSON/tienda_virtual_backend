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
    public class UsuarioRolDB : IEntityTypeConfiguration<UsuarioRol>
    {
        public void Configure(EntityTypeBuilder<UsuarioRol> builder)
        {
            builder.ToTable("usuario_rol", "xqm_seguridad");
            builder.HasKey(e => new { e.UsuarioId, e.RolId });

            builder.Property(e => e.UsuarioId)
                .HasColumnName("usuario_id");

            builder.Property(e => e.RolId)
                .HasColumnName("rol_id")
                .HasColumnType("int2");

            builder.HasOne(e => e.Usuario)
                .WithMany(u => u.UsuarioRoles)
                .HasForeignKey(e => e.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_usuario_rol_usuario");

            builder.HasOne(e => e.Rol)
                .WithMany(r => r.UsuarioRoles)
                .HasForeignKey(e => e.RolId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_usuario_rol_rol");
        }
    }
}
