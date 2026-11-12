using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Atividade5.Api.Erros;

/// <summary>
/// Escreve respostas de erro no formato padrão RFC 9457 (application/problem+json),
/// o mesmo formato usado pelos controllers ao chamar Problem(...).
/// </summary>
public static class RespostaDeErro
{
    public static Task EscreverAsync(HttpContext contexto, int status, string titulo, string detalhe)
    {
        var fabrica = contexto.RequestServices.GetRequiredService<ProblemDetailsFactory>();
        var problema = fabrica.CreateProblemDetails(contexto, status, titulo, detail: detalhe,
            instance: contexto.Request.Path);

        contexto.Response.StatusCode = status;
        return contexto.Response.WriteAsJsonAsync(problema, (JsonSerializerOptions?)null, "application/problem+json");
    }
}
