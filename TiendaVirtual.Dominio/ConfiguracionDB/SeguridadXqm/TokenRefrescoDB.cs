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
    public class TokenRefrescoDB : IEntityTypeConfiguration<TokenRefresco>
    {
        public void Configure(EntityTypeBuilder<TokenRefresco> builder)
        {
            builder.ToTable("token_refresco", "xqm_seguridad");
            builder.HasKey(e => e.TokenId);

            builder.Property(e => e.TokenId)
                .HasColumnName("token_id");

            builder.Property(e => e.UsuarioId)
                .HasColumnName("usuario_id")
                .IsRequired();

            builder.Property(e => e.TokenHash)
                .HasColumnName("token_hash")
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(e => e.ExpiraEn)
                .HasColumnName("expira_en");

            builder.Property(e => e.Revocado)
                .HasColumnName("revocado");

            builder.Property(e => e.FechaEmision)
                .HasColumnName("fecha_emision");

            builder.Property(e => e.DireccionIp)
                .HasColumnName("direccion_ip")
                .HasMaxLength(45);

            builder.Property(e => e.AgenteUsuario)
                .HasColumnName("agente_usuario")
                .HasMaxLength(500);

            builder.HasIndex(e => e.UsuarioId).HasDatabaseName("idx_token_usuario");
            builder.HasIndex(e => e.TokenHash).HasDatabaseName("idx_token_hash");

            builder.HasOne(e => e.Usuario)
                .WithMany(u => u.TokensRefresco)
                .HasForeignKey(e => e.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_token_usuario");
        }
    }
}
