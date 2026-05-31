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
    public class SuscripcionDB : IEntityTypeConfiguration<Suscripcion>
    {
        public void Configure(EntityTypeBuilder<Suscripcion> builder)
        {
            builder.ToTable("suscripcion", "xqm_vendedor");
            builder.HasKey(e => e.SuscripcionId);

            builder.Property(e => e.SuscripcionId)
                .HasColumnName("suscripcion_id");

            builder.Property(e => e.VendedorId)
                .HasColumnName("vendedor_id")
                .IsRequired();

            builder.Property(e => e.PlanId)
                .HasColumnName("plan_id")
                .IsRequired();

            builder.Property(e => e.Estado)
                .HasConversion<short>()
                .HasColumnType("int2")
                .HasColumnName("estado")
                .IsRequired();

            builder.Property(e => e.MesesGratisOtorgados)
                .HasColumnName("meses_gratis_otorgados")
                .HasColumnType("int2");

            builder.Property(e => e.PrecioPersonalizado)
                .HasColumnName("precio_personalizado")
                .HasColumnType("numeric(10,2)");

            builder.Property(e => e.CuponId)
                .HasColumnName("cupon_id");

            builder.Property(e => e.PruebaTerminaEn)
                .HasColumnName("prueba_termina_en");

            builder.Property(e => e.PeriodoInicio)
                .HasColumnName("periodo_inicio");

            builder.Property(e => e.PeriodoFin)
                .HasColumnName("periodo_fin");

            builder.HasIndex(e => e.VendedorId)
                .HasDatabaseName("idx_suscripcion_vendedor");

            builder.HasOne(e => e.Vendedor)
                .WithMany(v => v.Suscripciones)
                .HasForeignKey(e => e.VendedorId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_suscripcion_vendedor");

            builder.HasOne(e => e.Plan)
                .WithMany(p => p.Suscripciones)
                .HasForeignKey(e => e.PlanId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_suscripcion_plan");

            builder.HasOne(e => e.Cupon)
                .WithMany(c => c.Suscripciones)
                .HasForeignKey(e => e.CuponId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_suscripcion_cupon");
        }
    }
}
