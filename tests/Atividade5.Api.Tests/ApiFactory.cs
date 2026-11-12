using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Atividade5.Api.Tests;

/// <summary>Sobe a API em memória, com banco InMemory isolado para cada classe de teste.</summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    public const string EmailAdmin = "admin@atividade5.com";
    public const string SenhaAdmin = "Admin@123";

    private readonly string _nomeDoBanco = $"Testes-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("BancoDeDados:Provedor", "InMemory");
        builder.UseSetting("BancoDeDados:NomeEmMemoria", _nomeDoBanco);
        builder.UseSetting("Seguranca:TentativasDeLoginPorMinuto", "1000");
    }
}
