using Atividade5.Api.Models;
using Atividade5.Api.Seguranca;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Atividade5.Api.Data;

/// <summary>
/// Cria/atualiza o banco (migrations) e cadastra o administrador inicial
/// definido na seção "AdminPadrao" da configuração.
/// </summary>
public static class DbInicializador
{
    public static async Task InicializarAsync(IServiceProvider services)
    {
        var contexto = services.GetRequiredService<AppDbContext>();
        var configuracao = services.GetRequiredService<IConfiguration>();
        var hasher = services.GetRequiredService<IPasswordHasher<Usuario>>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DbInicializador));

        if (contexto.Database.IsRelational())
            await contexto.Database.MigrateAsync();
        else
            await contexto.Database.EnsureCreatedAsync();

        var email = configuracao["AdminPadrao:Email"]?.Trim().ToLowerInvariant();
        var senha = configuracao["AdminPadrao:Senha"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senha))
            return;

        if (await contexto.Usuarios.AnyAsync(u => u.Email == email))
            return;

        var administrador = new Usuario
        {
            Nome = configuracao["AdminPadrao:Nome"] ?? "Administrador",
            Email = email,
            Perfil = Perfis.Administrador
        };
        administrador.SenhaHash = hasher.HashPassword(administrador, senha);

        contexto.Usuarios.Add(administrador);
        await contexto.SaveChangesAsync();
        logger.LogInformation("Administrador padrão {Email} criado.", email);
    }
}
