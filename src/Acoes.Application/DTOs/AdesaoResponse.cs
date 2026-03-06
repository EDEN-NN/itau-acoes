namespace Acoes.Application.DTOs;

public class AdesaoResponse
{
    public long ClienteId { get; set; }
    public string NumeroConta { get; set; } = string.Empty;
    public string Mensagem { get; set; } = string.Empty;
}
