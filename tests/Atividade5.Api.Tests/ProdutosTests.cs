using System.Net;
using System.Net.Http.Json;

namespace Atividade5.Api.Tests;

public class ProdutosTests(ApiFactory fabrica) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _cliente = fabrica.CreateClient();

    [Fact]
    public async Task Listar_SemToken_Retorna200()
    {
        var resposta = await _cliente.GetAsync("/api/produtos");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal("application/json", resposta.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Criar_SemToken_Retorna401()
    {
        var resposta = await _cliente.PostAsJsonAsync("/api/produtos", new { nome = "Monitor", preco = 900m, estoque = 2 });

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task Criar_ComUsuarioAutenticado_Retorna201ComLocation()
    {
        var (_, _, token) = await _cliente.CriarUsuarioComumAsync();

        var resposta = await _cliente.EnviarAsync(HttpMethod.Post, "/api/produtos", token,
            new { nome = "Monitor", descricao = "24 polegadas", preco = 900m, estoque = 2 });

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        Assert.NotNull(resposta.Headers.Location);
    }

    [Fact]
    public async Task Criar_ComDadosInvalidos_Retorna400()
    {
        var (_, _, token) = await _cliente.CriarUsuarioComumAsync();

        var resposta = await _cliente.EnviarAsync(HttpMethod.Post, "/api/produtos", token,
            new { nome = "", preco = -1m, estoque = -5 });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task Atualizar_SemToken_Retorna401()
    {
        var resposta = await _cliente.PutAsJsonAsync("/api/produtos/1", new { nome = "Alterado", preco = 1m, estoque = 1 });

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }
}
