using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Dominio.Modelo.CatalogoXqm;

namespace TiendaVirtual.Dominio.ConfiguracionDB.CatalogoXqm
{
    public class StockDB : IEntityTypeConfiguration<Stock>
    {
        public void Configure(EntityTypeBuilder<Stock> builder)
        {
            builder.ToTable("stock", "xqm_catalogo");
            builder.HasKey(e => e.VarianteId);

            builder.Property(e => e.VarianteId)
                .HasColumnName("variante_id");

            builder.Property(e => e.CantidadDisponible)
                .HasColumnName("cantidad_disponible");

            builder.Property(e => e.CantidadReservada)
                .HasColumnName("cantidad_reservada");

            builder.Property(e => e.UmbralStockBajo)
                .HasColumnName("umbral_stock_bajo");

            builder.HasOne(e => e.Variante)
                .WithOne(v => v.Stock!)
                .HasForeignKey<Stock>(e => e.VarianteId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_stock_variante");
        }
    }
}
