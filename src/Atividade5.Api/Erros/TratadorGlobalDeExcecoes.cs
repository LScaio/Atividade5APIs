using Microsoft.AspNetCore.Diagnostics;

namespace Atividade5.Api.Erros;

/// <summary>
/// Captura qualquer exceção não tratada, registra no log (para depuração) e devolve um JSON padronizado,
/// sem expor detalhes internos (stack trace, SQL, etc.) fora do ambiente de desenvolvimento.
/// </summary>
public class TratadorGlobalDeExcecoes(ILogger<TratadorGlobalDeExcecoes> logger, IHostEnvironment ambiente)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext contexto, Exception excecao, CancellationToken cancellationToken)
    {
        var (status, titulo) = excecao switch
        {
            BadHttpRequestException => (StatusCodes.Status400BadRequest, "Requisição inválida"),
            OperationCanceledException => (499, "Requisição cancelada"),
            _ => (StatusCodes.Status500InternalServerError, "Erro interno no servidor")
        };

        logger.LogError(excecao, "Erro ao processar {Metodo} {Caminho}", contexto.Request.Method, contexto.Request.Path);

        var detalhe = ambiente.IsDevelopment()
            ? excecao.Message
            : "Ocorreu um erro inesperado. Tente novamente mais tarde.";

        await RespostaDeErro.EscreverAsync(contexto, status, titulo, detalhe);
        return true;
    }
}
