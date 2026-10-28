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

            // A6: evita sobreventa por condición de carrera. Dos compras
            // concurrentes pueden leer el mismo "CantidadDisponible" antes de
            // que la primera confirme su descuento; sin esto, la segunda
            // pisaría el cambio de la primera (lost update) y se vendería más
            // stock del que existe. `xmin` es la columna de versión de fila
            // nativa de Postgres (ya existe en toda tabla, no requiere
            // migración ni columna nueva): si la fila cambió desde que se
            // leyó, EF lanza DbUpdateConcurrencyException en vez de
            // sobrescribir a ciegas.
            builder.Property<uint>("xmin")
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsRowVersion();
        }
    }
}
