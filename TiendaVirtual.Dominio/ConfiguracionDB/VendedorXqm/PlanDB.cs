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
    public class PlanDB : IEntityTypeConfiguration<Plan>
    {
        public void Configure(EntityTypeBuilder<Plan> builder)
        {
            builder.ToTable("plan", "xqm_vendedor");
            builder.HasKey(e => e.PlanId);

            builder.Property(e => e.PlanId)
                .HasColumnName("plan_id");

            builder.Property(e => e.Codigo)
                .HasColumnName("codigo")
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(e => e.Nombre)
                .HasColumnName("nombre")
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(e => e.Descripcion)
                .HasColumnName("descripcion")
                .HasMaxLength(500);

            builder.Property(e => e.Precio)
                .HasColumnName("precio")
                .HasColumnType("numeric(10,2)");

            builder.Property(e => e.Periodo)
                .HasConversion<short>()
                .HasColumnType("int2")
                .HasColumnName("periodo")
                .IsRequired();

            builder.Property(e => e.MaxProductos)
                .HasColumnName("max_productos");

            builder.Property(e => e.TasaComision)
                .HasColumnName("tasa_comision")
                .HasColumnType("numeric(5,2)");

            builder.Property(e => e.Activo)
                .HasColumnName("activo");

            builder.HasIndex(e => e.Codigo)
                .IsUnique()
                .HasDatabaseName("uq_plan_codigo");
        }
    }
}
