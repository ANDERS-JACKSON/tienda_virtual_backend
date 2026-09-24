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
    public class VendedorDB : IEntityTypeConfiguration<Vendedor>
    {
        public void Configure(EntityTypeBuilder<Vendedor> builder)
        {
            builder.ToTable("vendedor", "xqm_vendedor");
            builder.HasKey(e => e.VendedorId);

            builder.Property(e => e.VendedorId)
                .HasColumnName("vendedor_id");

            builder.Property(e => e.UsuarioId)
                .HasColumnName("usuario_id")
                .IsRequired();

            builder.Property(e => e.NombreTienda)
                .HasColumnName("nombre_tienda")
                .HasMaxLength(150)
                .IsRequired();

            builder.Property(e => e.SlugTienda)
                .HasColumnName("slug_tienda")
                .HasMaxLength(150)
                .IsRequired();

            builder.Property(e => e.Biografia)
                .HasColumnName("biografia")
                .HasMaxLength(1000);

            builder.Property(e => e.LogoUrl)
                .HasColumnName("logo_url")
                .HasMaxLength(500);

            builder.Property(e => e.BannerUrl)
                .HasColumnName("banner_url")
                .HasMaxLength(500);

            builder.Property(e => e.Estado)
                .HasConversion<short>()
                .HasColumnType("int2")
                .HasColumnName("estado")
                .IsRequired();

            builder.Property(e => e.CalificacionPromedio)
                .HasColumnName("calificacion_promedio")
                .HasColumnType("numeric(3,2)");

            builder.Property(e => e.TotalVentas)
                .HasColumnName("total_ventas");

            builder.Property(e => e.InvitadoPor)
                .HasColumnName("invitado_por");

            builder.Property(e => e.NumeroYape)
                .HasColumnName("numero_yape")
                .HasMaxLength(20);

            builder.Property(e => e.NumeroWhatsapp)
                .HasColumnName("numero_whatsapp")
                .HasMaxLength(20);

            builder.Property(e => e.DistritoId)
                .HasColumnName("distrito_id")
                .HasMaxLength(6);

            builder.Property(e => e.UbicacionDepartamento)
                .HasColumnName("ubicacion_departamento")
                .HasMaxLength(100);

            builder.Property(e => e.UbicacionProvincia)
                .HasColumnName("ubicacion_provincia")
                .HasMaxLength(100);

            builder.Property(e => e.UbicacionDistrito)
                .HasColumnName("ubicacion_distrito")
                .HasMaxLength(100);

            builder.Property(e => e.VendePatrones)
                .HasColumnName("vende_patrones");

            builder.HasIndex(e => e.UsuarioId).IsUnique().HasDatabaseName("uq_vendedor_usuario");
            builder.HasIndex(e => e.SlugTienda).IsUnique().HasDatabaseName("uq_vendedor_slug");
            builder.HasIndex(e => e.Estado).HasDatabaseName("idx_vendedor_estado");
            builder.HasIndex(e => e.DistritoId).HasDatabaseName("idx_vendedor_distrito");

            builder.HasOne(e => e.Usuario)
                .WithOne(u => u.Vendedor!)
                .HasForeignKey<Vendedor>(e => e.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_vendedor_usuario");

            builder.HasOne(e => e.DistritoNav)
                .WithMany()
                .HasForeignKey(e => e.DistritoId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_vendedor_distrito")
                .IsRequired(false);

            builder.HasOne(e => e.InvitadoPorVendedor)
                .WithMany(v => v.VendedoresInvitados)
                .HasForeignKey(e => e.InvitadoPor)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_vendedor_invitado");
        }
    }
}
