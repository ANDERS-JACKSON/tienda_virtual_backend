using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TiendaVirtual.Dominio.Servicios.Sistema;
using TiendaVirtual.Intercambio;
using TiendaVirtual.Intercambio.Dto.Sistema;

namespace TiendaVirtual.Api.Controllers.Sistema
{
    [ApiController]
    [Route("api/[controller]")]
    public class EnumeracionController : ControllerBase
    {
        private readonly IEnumeracionServicio _servicio;

        public EnumeracionController(IEnumeracionServicio servicio)
        {
            _servicio = servicio;
        }

        /// <summary>Lista genérica de valores de enumeración por grupo (p. ej. tipo-documento-identidad).</summary>
        [HttpGet("{grupo}")]
        [AllowAnonymous]
        public async Task<ActionResult<ResultadoOperacion<List<EnumeracionDto>>>> ListarPorGrupo(string grupo)
        {
            var r = await _servicio.ListarPorGrupoAsync(grupo);
            return r.Exito ? Ok(r) : NotFound(r);
        }

        /// <summary>Tipos de documento con etiqueta y longitud esperada para validación en formularios.</summary>
        [HttpGet("tipos-documento-identidad")]
        [AllowAnonymous]
        public async Task<ActionResult<ResultadoOperacion<List<TipoDocumentoIdentidadDto>>>> ListarTiposDocumentoIdentidad()
        {
            var r = await _servicio.ListarTiposDocumentoIdentidadAsync();
            return r.Exito ? Ok(r) : BadRequest(r);
        }
    }
}
