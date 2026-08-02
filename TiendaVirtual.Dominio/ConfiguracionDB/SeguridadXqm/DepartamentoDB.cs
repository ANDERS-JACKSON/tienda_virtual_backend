using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TiendaVirtual.Dominio.Modelo.SeguridadXqm;

namespace TiendaVirtual.Dominio.ConfiguracionDB.SeguridadXqm
{
    public class DepartamentoDB : IEntityTypeConfiguration<Departamento>
    {
        public void Configure(EntityTypeBuilder<Departamento> builder)
        {
            builder.ToTable("departamento", "xqm_seguridad");
            builder.HasKey(e => e.DepartamentoId);

            builder.Property(e => e.DepartamentoId)
                .HasColumnName("departamento_id")
                .HasMaxLength(2)
                .IsRequired();

            builder.Property(e => e.Nombre)
                .HasColumnName("nombre")
                .HasMaxLength(100)
                .IsRequired();
        }
    }
}
