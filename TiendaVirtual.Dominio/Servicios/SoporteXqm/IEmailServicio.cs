namespace TiendaVirtual.Dominio.Servicios.SoporteXqm
{
    public interface IEmailServicio
    {
        Task<bool> EnviarAsync(string destinatario, string nombreDestinatario,
            string asunto, string cuerpoHtml);
    }
}
