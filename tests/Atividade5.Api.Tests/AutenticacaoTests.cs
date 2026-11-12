using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Atividade5.Api.Data;
using Atividade5.Api.Seguranca;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Atividade5.Api.Tests;

public class AutenticacaoTests(ApiFactory fabrica) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _cliente = fabrica.CreateClient();

    [Fact]
    public async Task Registrar_ArmazenaSomenteHashDaSenha_ENaoExpoeSenhaNaResposta()
    {
        var email = $"hash-{Guid.NewGuid():N}@teste.com";

        var resposta = await _cliente.PostAsJsonAsync("/api/auth/registrar", new { nome = "Fulano", email, senha = "Senha@123" });

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        var corpo = await resposta.Content.ReadAsStringAsync();
        Assert.DoesNotContain("senha", corpo, StringComparison.OrdinalIgnoreCase);

        using var escopo = fabrica.Services.CreateScope();
        var usuario = await escopo.ServiceProvider.GetRequiredService<AppDbContext>().Usuarios.SingleAsync(u => u.Email == email);
        Assert.NotEqual("Senha@123", usuario.SenhaHash);
        Assert.DoesNotContain("Senha@123", usuario.SenhaHash);
    }

    [Fact]
    public async Task Registrar_TentandoSeCadastrarComoAdministrador_RecebePerfilUsuario()
    {
        var resposta = await _cliente.PostAsJsonAsync("/api/auth/registrar", new
        {
            nome = "Espertinho",
            email = $"esperto-{Guid.NewGuid():N}@teste.com",
            senha = "Senha@123",
            perfil = Perfis.Administrador
        });

        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        Assert.Equal(Perfis.Usuario, corpo.GetProperty("perfil").GetString());
    }

    [Fact]
    public async Task Registrar_EmailDuplicado_Retorna409()
    {
        var resposta = await _cliente.PostAsJsonAsync("/api/auth/registrar",
            new { nome = "Outro Admin", email = ApiFactory.EmailAdmin.ToUpperInvariant(), senha = "Senha@123" });

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
    }

    [Fact]
    public async Task Registrar_SenhaFraca_Retorna400()
    {
        var resposta = await _cliente.PostAsJsonAsync("/api/auth/registrar",
            new { nome = "Fulano", email = "fraca@teste.com", senha = "123" });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task Login_SenhaIncorreta_Retorna401()
    {
        var resposta = await _cliente.PostAsJsonAsync("/api/auth/login",
            new { email = ApiFactory.EmailAdmin, senha = "SenhaErrada1" });

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task Login_Valido_RetornaTokenJwtComPerfilENenhumDadoSensivel()
    {
        var token = await _cliente.LoginComoAdminAsync();

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Equal(Perfis.Administrador, jwt.Claims.Single(c => c.Type == ClaimsDoToken.Perfil).Value);
        Assert.True(jwt.ValidTo > DateTime.UtcNow);
        Assert.DoesNotContain(jwt.Claims, c => c.Value.Contains(ApiFactory.SenhaAdmin));
    }

    [Fact]
    public async Task Me_ComToken_RetornaUsuarioAutenticado()
    {
        var resposta = await _cliente.EnviarAsync(HttpMethod.Get, "/api/auth/me", await _cliente.LoginComoAdminAsync());

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(ApiFactory.EmailAdmin, corpo.GetProperty("email").GetString());
    }

    [Fact]
    public async Task ListarUsuarios_ComUsuarioComum_Retorna403()
    {
        var (_, _, token) = await _cliente.CriarUsuarioComumAsync();

        var resposta = await _cliente.EnviarAsync(HttpMethod.Get, "/api/usuarios", token);

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task Login_ExcedendoLimiteDeTentativas_Retorna429()
    {
        await using var fabricaComLimite = new ApiFactory().WithWebHostBuilder(b =>
            b.UseSetting("Seguranca:TentativasDeLoginPorMinuto", "3"));
        var cliente = fabricaComLimite.CreateClient();

        var status = new List<HttpStatusCode>();
        for (var i = 0; i < 4; i++)
            status.Add((await cliente.PostAsJsonAsync("/api/auth/login", new { email = "x@teste.com", senha = "Errada123" })).StatusCode);

        Assert.Equal([HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized,
            HttpStatusCode.TooManyRequests], status);
    }
}
