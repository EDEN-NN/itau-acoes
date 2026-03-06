namespace Acoes.Application.DTOs;

public class InicializarSistemaResponse
{
    public bool ContaMasterCriada { get; set; }
    public string NumeroConta { get; set; } = string.Empty;
    public string Mensagem { get; set; } = string.Empty;
}
