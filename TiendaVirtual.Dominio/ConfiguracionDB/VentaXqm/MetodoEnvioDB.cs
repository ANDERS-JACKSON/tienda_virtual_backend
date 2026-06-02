using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TiendaVirtual.Dominio.Modelo.VentaXqm;

namespace TiendaVirtual.Dominio.ConfiguracionDB.VentaXqm
{
    public class MetodoEnvioDB : IEntityTypeConfiguration<MetodoEnvio>
    {
        public void Configure(EntityTypeBuilder<MetodoEnvio> builder)
        {
            builder.ToTable("metodo_envio", "xqm_venta");
            builder.HasKey(e => e.MetodoEnvioId);

            builder.Property(e => e.MetodoEnvioId)
                .HasColumnName("metodo_envio_id");

            builder.Property(e => e.Codigo)
                .HasColumnName("codigo")
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(e => e.Nombre)
                .HasColumnName("nombre")
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(e => e.Descripcion)
                .HasColumnName("descripcion")
                .HasMaxLength(500);

            builder.Property(e => e.MontoBase)
                .HasColumnName("monto_base")
                .HasColumnType("numeric(10,2)");

            builder.Property(e => e.TiempoEstimadoDias)
                .HasColumnName("tiempo_estimado_dias");

            builder.Property(e => e.Activo)
                .HasColumnName("activo");

            builder.Property(e => e.Orden)
                .HasColumnName("orden");

            builder.HasIndex(e => e.Codigo)
                .IsUnique()
                .HasDatabaseName("uq_metodo_envio_codigo");
        }
    }
}
