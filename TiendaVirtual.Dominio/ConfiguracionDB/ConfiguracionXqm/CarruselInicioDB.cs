using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TiendaVirtual.Dominio.Modelo.ConfiguracionXqm;

namespace TiendaVirtual.Dominio.ConfiguracionDB.ConfiguracionXqm
{
    public class CarruselInicioDB : IEntityTypeConfiguration<CarruselInicio>
    {
        public void Configure(EntityTypeBuilder<CarruselInicio> builder)
        {
            builder.ToTable("carrusel_inicio", "xqm_configuracion");
            builder.HasKey(e => e.CarruselInicioId);

            builder.Property(e => e.CarruselInicioId)
                .HasColumnName("carrusel_inicio_id");

            builder.Property(e => e.ImagenUrl)
                .HasColumnName("imagen_url")
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(e => e.Titulo)
                .HasColumnName("titulo")
                .HasMaxLength(120);

            builder.Property(e => e.Subtitulo)
                .HasColumnName("subtitulo")
                .HasMaxLength(300);

            builder.Property(e => e.EnlaceUrl)
                .HasColumnName("enlace_url")
                .HasMaxLength(500);

            builder.Property(e => e.TextoBoton)
                .HasColumnName("texto_boton")
                .HasMaxLength(80);

            builder.Property(e => e.Activo)
                .HasColumnName("activo");

            builder.Property(e => e.Orden)
                .HasColumnName("orden");
        }
    }
}
