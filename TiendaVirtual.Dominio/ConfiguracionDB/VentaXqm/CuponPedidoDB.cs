using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TiendaVirtual.Dominio.Modelo.VentaXqm;

namespace TiendaVirtual.Dominio.ConfiguracionDB.VentaXqm
{
    public class CuponPedidoDB : IEntityTypeConfiguration<CuponPedido>
    {
        public void Configure(EntityTypeBuilder<CuponPedido> builder)
        {
            builder.ToTable("cupon_pedido", "xqm_venta");
            builder.HasKey(e => e.CuponPedidoId);

            builder.Property(e => e.CuponPedidoId).HasColumnName("cupon_pedido_id");
            builder.Property(e => e.Codigo)
                .HasColumnName("codigo")
                .HasMaxLength(40)
                .IsRequired();
            builder.Property(e => e.TipoDescuento)
                .HasConversion<short>()
                .HasColumnType("int2")
                .HasColumnName("tipo_descuento")
                .IsRequired();
            builder.Property(e => e.ValorDescuento)
                .HasColumnName("valor_descuento")
                .HasColumnType("numeric(10,2)");
            builder.Property(e => e.MontoMinimo)
                .HasColumnName("monto_minimo")
                .HasColumnType("numeric(10,2)");
            builder.Property(e => e.UsosMaximos).HasColumnName("usos_maximos");
            builder.Property(e => e.UsosRealizados).HasColumnName("usos_realizados");
            builder.Property(e => e.ValidoHasta).HasColumnName("valido_hasta");
            builder.Property(e => e.Activo).HasColumnName("activo");
            builder.Property(e => e.Descripcion)
                .HasColumnName("descripcion")
                .HasMaxLength(200);
            builder.Property(e => e.FechaCreacion).HasColumnName("fecha_creacion");
        }
    }
}
