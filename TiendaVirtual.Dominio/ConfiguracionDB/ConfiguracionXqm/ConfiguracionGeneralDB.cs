using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiendaVirtual.Dominio.Modelo.ConfiguracionXqm;

namespace TiendaVirtual.Dominio.ConfiguracionDB.ConfiguracionXqm
{
    public class ConfiguracionGeneralDB : IEntityTypeConfiguration<Configuracion>
    {
        public void Configure(EntityTypeBuilder<Configuracion> builder)
        {
            builder.ToTable("configuracion", "xqm_configuracion");
            builder.HasKey(e => e.ConfiguracionId);

            builder.Property(e => e.ConfiguracionId)
                .HasColumnName("configuracion_id");

            builder.Property(e => e.TokenDuracionMinutos)
                .HasColumnName("token_duracion_minutos");

            builder.Property(e => e.DiasLiberacionPago)
                .HasColumnName("dias_liberacion_pago");

            builder.Property(e => e.ComisionPorDefecto)
                .HasColumnName("comision_por_defecto")
                .HasColumnType("numeric(5,2)");

            builder.Property(e => e.Anio)
                .HasColumnName("anio");

            builder.Property(e => e.AnioNombre)
                .HasColumnName("anio_nombre")
                .HasMaxLength(150);
        }
    }
}
