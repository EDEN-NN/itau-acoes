using System;
using System.Threading;
using System.Threading.Tasks;
using Acoes.Application.Common;
using Acoes.Application.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Acoes.Api.Middleware;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        int statusCode;
        ApiResult<object> resultado;

        if (exception is DomainException domainEx)
        {
            
            _logger.LogWarning("Erro de domínio [{Codigo}]: {Mensagem}", domainEx.Codigo, domainEx.Message);

            statusCode = domainEx.HttpStatusCode;
            resultado = ApiResult<object>.Failure(domainEx.Message, domainEx.Codigo);
        }
        else
        {
            
            _logger.LogError(exception, "Erro inesperado em {Method} {Path}",
                httpContext.Request.Method, httpContext.Request.Path);

            statusCode = StatusCodes.Status500InternalServerError;
            resultado = ApiResult<object>.Failure(
                "Ocorreu um erro interno. Tente novamente mais tarde.",
                "ERRO_INTERNO");
        }

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(resultado, cancellationToken);

        return true;
    }
}
