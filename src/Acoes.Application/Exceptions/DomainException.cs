using System;

namespace Acoes.Application.Exceptions;

public class DomainException : Exception
{
    public string Codigo { get; }
    public int HttpStatusCode { get; }

    public DomainException(string mensagem, string codigo, int httpStatusCode = 400)
        : base(mensagem)
    {
        Codigo = codigo;
        HttpStatusCode = httpStatusCode;
    }
}
