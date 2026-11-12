using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Atividade5.Api.Seguranca;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Atividade5.Api.Tests;

public static class ApiHelpers
{
    public static async Task<string> LoginAsync(this HttpClient cliente, string email, string senha)
    {
        var resposta = await cliente.PostAsJsonAsync("/api/auth/login", new { email, senha });
        resposta.EnsureSuccessStatusCode();
        var json = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("token").GetString()!;
    }

    public static Task<string> LoginComoAdminAsync(this HttpClient cliente) =>
        cliente.LoginAsync(ApiFactory.EmailAdmin, ApiFactory.SenhaAdmin);

    /// <summary>Cadastra um usuário comum (perfil "Usuario") e devolve (id, e-mail, token).</summary>
    public static async Task<(int Id, string Email, string Token)> CriarUsuarioComumAsync(this HttpClient cliente)
    {
        var email = $"usuario-{Guid.NewGuid():N}@teste.com";
        const string senha = "Senha@123";

        var resposta = await cliente.PostAsJsonAsync("/api/auth/registrar", new { nome = "Usuário Teste", email, senha });
        resposta.EnsureSuccessStatusCode();
        var id = (await resposta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

        return (id, email, await cliente.LoginAsync(email, senha));
    }

    public static async Task<int> CriarProdutoAsync(this HttpClient cliente, string token)
    {
        using var requisicao = new HttpRequestMessage(HttpMethod.Post, "/api/produtos")
        {
            Content = JsonContent.Create(new { nome = "Produto de teste", descricao = "Criado no teste", preco = 10.5m, estoque = 1 })
        };
        requisicao.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resposta = await cliente.SendAsync(requisicao);
        resposta.EnsureSuccessStatusCode();
        return (await resposta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
    }

    public static Task<HttpResponseMessage> EnviarAsync(this HttpClient cliente, HttpMethod metodo, string url,
        string? token = null, object? corpo = null)
    {
        var requisicao = new HttpRequestMessage(metodo, url);
        if (token is not null)
            requisicao.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (corpo is not null)
            requisicao.Content = JsonContent.Create(corpo);
        return cliente.SendAsync(requisicao);
    }

    /// <summary>Monta um token "na mão" para simular tokens expirados ou forjados.</summary>
    public static string GerarTokenManual(ApiFactory fabrica, string chave, int idUsuario, string perfil, DateTime expiraEm)
    {
        var jwt = fabrica.Services.GetRequiredService<IOptions<JwtSettings>>().Value;
        var credenciais = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(chave)), SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(jwt.Emissor, jwt.Audiencia,
            [
                new Claim(ClaimsDoToken.Id, idUsuario.ToString()),
                new Claim(ClaimsDoToken.Nome, "Atacante"),
                new Claim(ClaimsDoToken.Email, "atacante@teste.com"),
                new Claim(ClaimsDoToken.Perfil, perfil)
            ],
            notBefore: expiraEm.AddHours(-2), expires: expiraEm, signingCredentials: credenciais);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static string ChaveDaApi(this ApiFactory fabrica) =>
        fabrica.Services.GetRequiredService<IOptions<JwtSettings>>().Value.Chave;
}
