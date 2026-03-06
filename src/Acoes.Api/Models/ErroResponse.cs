namespace Acoes.Api.Models;

public class ErroResponse
{
    public string Erro { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;

    public ErroResponse(string erro, string codigo)
    {
        Erro = erro;
        Codigo = codigo;
    }
}
