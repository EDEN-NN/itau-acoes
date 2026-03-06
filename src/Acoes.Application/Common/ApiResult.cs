using System.Text.Json.Serialization;

namespace Acoes.Application.Common;

public class ApiResult<T>
{
    public bool Sucesso { get; private set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public T? Dados { get; private set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Erro { get; private set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Codigo { get; private set; }

    private ApiResult() { }

    public static ApiResult<T> Success(T dados) => new()
    {
        Sucesso = true,
        Dados = dados
    };

    public static ApiResult<T> Failure(string erro, string codigo) => new()
    {
        Sucesso = false,
        Erro = erro,
        Codigo = codigo
    };
}
