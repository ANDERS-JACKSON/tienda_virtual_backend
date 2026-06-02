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
    public class CarritoDB : IEntityTypeConfiguration<Carrito>
    {
        public void Configure(EntityTypeBuilder<Carrito> builder)
        {
            builder.ToTable("carrito", "xqm_venta");
            builder.HasKey(e => e.CarritoId);

            builder.Property(e => e.CarritoId)
                .HasColumnName("carrito_id");

            builder.Property(e => e.UsuarioId)
                .HasColumnName("usuario_id")
                .IsRequired();

            builder.Property(e => e.FechaActualizacion)
                .HasColumnName("fecha_actualizacion");

            builder.HasIndex(e => e.UsuarioId)
                .IsUnique()
                .HasDatabaseName("uq_carrito_usuario");

            builder.HasOne(e => e.Usuario)
                .WithMany(u => u.Carritos)
                .HasForeignKey(e => e.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_carrito_usuario");
        }
    }
}
