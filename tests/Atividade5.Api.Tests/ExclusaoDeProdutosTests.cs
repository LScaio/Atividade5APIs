using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Atividade5.Api.Seguranca;

namespace Atividade5.Api.Tests;

/// <summary>
/// Regra principal da atividade: somente usuários AUTENTICADOS e com PERMISSÃO podem excluir.
/// </summary>
public class ExclusaoDeProdutosTests(ApiFactory fabrica) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _cliente = fabrica.CreateClient();

    [Fact]
    public async Task Excluir_SemToken_Retorna401EProdutoContinuaExistindo()
    {
        var produtoId = await _cliente.CriarProdutoAsync(await _cliente.LoginComoAdminAsync());

        var resposta = await _cliente.DeleteAsync($"/api/produtos/{produtoId}");

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
        Assert.Equal("application/problem+json", resposta.Content.Headers.ContentType?.MediaType);
        Assert.Equal(HttpStatusCode.OK, (await _cliente.GetAsync($"/api/produtos/{produtoId}")).StatusCode);
    }

    [Fact]
    public async Task Excluir_ComTokenMalformado_Retorna401()
    {
        var resposta = await _cliente.EnviarAsync(HttpMethod.Delete, "/api/produtos/1", token: "isto.nao.e-um-token");

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task Excluir_ComTokenExpirado_Retorna401()
    {
        var tokenExpirado = ApiHelpers.GerarTokenManual(fabrica, fabrica.ChaveDaApi(), 1, Perfis.Administrador,
            expiraEm: DateTime.UtcNow.AddMinutes(-1));

        var resposta = await _cliente.EnviarAsync(HttpMethod.Delete, "/api/produtos/1", tokenExpirado);

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("expirou", corpo.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Excluir_ComTokenForjadoComOutraChave_Retorna401EProdutoContinuaExistindo()
    {
        var produtoId = await _cliente.CriarProdutoAsync(await _cliente.LoginComoAdminAsync());
        var tokenForjado = ApiHelpers.GerarTokenManual(fabrica, "uma-chave-qualquer-que-o-atacante-inventou-123",
            1, Perfis.Administrador, DateTime.UtcNow.AddHours(1));

        var resposta = await _cliente.EnviarAsync(HttpMethod.Delete, $"/api/produtos/{produtoId}", tokenForjado);

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _cliente.GetAsync($"/api/produtos/{produtoId}")).StatusCode);
    }

    [Fact]
    public async Task Excluir_ComUsuarioAutenticadoSemPermissao_Retorna403EProdutoContinuaExistindo()
    {
        var (_, _, tokenUsuario) = await _cliente.CriarUsuarioComumAsync();
        var produtoId = await _cliente.CriarProdutoAsync(tokenUsuario);

        var resposta = await _cliente.EnviarAsync(HttpMethod.Delete, $"/api/produtos/{produtoId}", tokenUsuario);

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _cliente.GetAsync($"/api/produtos/{produtoId}")).StatusCode);
    }

    [Fact]
    public async Task Excluir_ComAdministrador_Retorna204ERemoveProduto()
    {
        var tokenAdmin = await _cliente.LoginComoAdminAsync();
        var produtoId = await _cliente.CriarProdutoAsync(tokenAdmin);

        var resposta = await _cliente.EnviarAsync(HttpMethod.Delete, $"/api/produtos/{produtoId}", tokenAdmin);

        Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _cliente.GetAsync($"/api/produtos/{produtoId}")).StatusCode);
    }

    [Fact]
    public async Task Excluir_ProdutoInexistente_ComAdministrador_Retorna404()
    {
        var resposta = await _cliente.EnviarAsync(HttpMethod.Delete, "/api/produtos/999999",
            await _cliente.LoginComoAdminAsync());

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task Excluir_UsuarioPromovidoAAdministrador_PodeExcluirApósNovoLogin()
    {
        var tokenAdmin = await _cliente.LoginComoAdminAsync();
        var (id, email, tokenAntigo) = await _cliente.CriarUsuarioComumAsync();
        var produtoId = await _cliente.CriarProdutoAsync(tokenAdmin);

        var promocao = await _cliente.EnviarAsync(HttpMethod.Put, $"/api/usuarios/{id}/perfil", tokenAdmin,
            new { perfil = Perfis.Administrador });
        Assert.Equal(HttpStatusCode.OK, promocao.StatusCode);

        // O token antigo (perfil "Usuario") deixa de valer...
        var comTokenAntigo = await _cliente.EnviarAsync(HttpMethod.Delete, $"/api/produtos/{produtoId}", tokenAntigo);
        Assert.Equal(HttpStatusCode.Unauthorized, comTokenAntigo.StatusCode);

        // ...e após novo login o usuário passa a ter a permissão de exclusão.
        var tokenNovo = await _cliente.LoginAsync(email, "Senha@123");
        var comTokenNovo = await _cliente.EnviarAsync(HttpMethod.Delete, $"/api/produtos/{produtoId}", tokenNovo);
        Assert.Equal(HttpStatusCode.NoContent, comTokenNovo.StatusCode);
    }

    [Fact]
    public async Task Excluir_AdministradorRebaixado_NaoPodeMaisUsarTokenAntigo()
    {
        var tokenAdmin = await _cliente.LoginComoAdminAsync();
        var (id, email, _) = await _cliente.CriarUsuarioComumAsync();
        await _cliente.EnviarAsync(HttpMethod.Put, $"/api/usuarios/{id}/perfil", tokenAdmin, new { perfil = Perfis.Administrador });
        var tokenComoAdmin = await _cliente.LoginAsync(email, "Senha@123");
        var produtoId = await _cliente.CriarProdutoAsync(tokenAdmin);

        await _cliente.EnviarAsync(HttpMethod.Put, $"/api/usuarios/{id}/perfil", tokenAdmin, new { perfil = Perfis.Usuario });
        var resposta = await _cliente.EnviarAsync(HttpMethod.Delete, $"/api/produtos/{produtoId}", tokenComoAdmin);

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _cliente.GetAsync($"/api/produtos/{produtoId}")).StatusCode);
    }
}
