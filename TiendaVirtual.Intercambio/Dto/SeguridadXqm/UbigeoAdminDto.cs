namespace TiendaVirtual.Intercambio.Dto.SeguridadXqm
{
    public class DepartamentoAdminDto
    {
        public string DepartamentoId { get; set; } = null!;
        public string Nombre { get; set; } = null!;
        public int TotalProvincias { get; set; }
    }

    public class ProvinciaAdminDto
    {
        public string ProvinciaId { get; set; } = null!;
        public string DepartamentoId { get; set; } = null!;
        public string NombreDepartamento { get; set; } = null!;
        public string Nombre { get; set; } = null!;
        public int TotalDistritos { get; set; }
    }

    public class DistritoAdminDto
    {
        public string DistritoId { get; set; } = null!;
        public string ProvinciaId { get; set; } = null!;
        public string NombreProvincia { get; set; } = null!;
        public string DepartamentoId { get; set; } = null!;
        public string NombreDepartamento { get; set; } = null!;
        public string Nombre { get; set; } = null!;
    }

    public class CrearDepartamentoDto
    {
        public string DepartamentoId { get; set; } = null!;
        public string Nombre { get; set; } = null!;
    }

    public class ActualizarDepartamentoDto
    {
        public string Nombre { get; set; } = null!;
    }

    public class CrearProvinciaDto
    {
        public string ProvinciaId { get; set; } = null!;
        public string DepartamentoId { get; set; } = null!;
        public string Nombre { get; set; } = null!;
    }

    public class ActualizarProvinciaDto
    {
        public string Nombre { get; set; } = null!;
    }

    public class CrearDistritoDto
    {
        public string DistritoId { get; set; } = null!;
        public string ProvinciaId { get; set; } = null!;
        public string Nombre { get; set; } = null!;
    }

    public class ActualizarDistritoDto
    {
        public string Nombre { get; set; } = null!;
    }
}
