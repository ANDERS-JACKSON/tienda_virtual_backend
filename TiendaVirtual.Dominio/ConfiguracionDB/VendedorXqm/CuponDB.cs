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
    public class CuponDB : IEntityTypeConfiguration<Cupon>
    {
        public void Configure(EntityTypeBuilder<Cupon> builder)
        {
            builder.ToTable("cupon", "xqm_vendedor");
            builder.HasKey(e => e.CuponId);

            builder.Property(e => e.CuponId)
                .HasColumnName("cupon_id");

            builder.Property(e => e.Codigo)
                .HasColumnName("codigo")
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(e => e.TipoDescuento)
                .HasConversion<short>()
                .HasColumnType("int2")
                .HasColumnName("tipo_descuento")
                .IsRequired();

            builder.Property(e => e.ValorDescuento)
                .HasColumnName("valor_descuento")
                .HasColumnType("numeric(10,2)");

            builder.Property(e => e.MesesGratis)
                .HasColumnName("meses_gratis")
                .HasColumnType("int2");

            builder.Property(e => e.UsosMaximos)
                .HasColumnName("usos_maximos");

            builder.Property(e => e.UsosRealizados)
                .HasColumnName("usos_realizados");

            builder.Property(e => e.ValidoHasta)
                .HasColumnName("valido_hasta");

            builder.Property(e => e.Activo)
                .HasColumnName("activo");

            builder.HasIndex(e => e.Codigo)
                .IsUnique()
                .HasDatabaseName("uq_cupon_codigo");
        }
    }
}
