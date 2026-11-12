using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Atividade5.Api.Data;
using Atividade5.Api.Erros;
using Atividade5.Api.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Atividade5.Api.Seguranca;

/// <summary>
/// Centraliza a configuração de AUTENTICAÇÃO (quem é você?) e AUTORIZAÇÃO (o que você pode fazer?).
/// </summary>
public static class ConfiguracaoDeSeguranca
{
    public static IServiceCollection AddSegurancaDaApi(this IServiceCollection services, IConfiguration configuration)
    {
        // 1) Configurações do JWT, validadas na inicialização: a API não sobe sem uma chave forte.
        services.AddOptions<JwtSettings>()
            .Bind(configuration.GetSection(JwtSettings.Secao))
            .Validate(j => !string.IsNullOrWhiteSpace(j.Emissor) && !string.IsNullOrWhiteSpace(j.Audiencia),
                "Configure Jwt:Emissor e Jwt:Audiencia.")
            .Validate(j => Encoding.UTF8.GetByteCount(j.Chave ?? string.Empty) >= 32,
                "Configure Jwt:Chave com pelo menos 32 caracteres (use variável de ambiente ou user-secrets).")
            .Validate(j => j.ExpiracaoEmMinutos > 0, "Jwt:ExpiracaoEmMinutos deve ser maior que zero.")
            .ValidateOnStart();

        services.AddSingleton<ITokenService, TokenService>();

        // Hash de senha com PBKDF2 (HMAC-SHA512, salt aleatório e 100.000 iterações) do ASP.NET Core Identity.
        services.AddSingleton<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();

        // 2) AUTENTICAÇÃO: todo request com "Authorization: Bearer {token}" tem o token validado.
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtSettings>>((opcoes, configuracaoJwt) =>
            {
                var jwt = configuracaoJwt.Value;

                // Mantém os nomes curtos das claims ("sub", "role"...) exatamente como estão no token.
                opcoes.MapInboundClaims = false;

                opcoes.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Emissor,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audiencia,
                    ValidateIssuerSigningKey = true,      // garante a integridade: token alterado = assinatura inválida
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Chave)),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    ValidateLifetime = true,              // token expirado é rejeitado
                    RequireExpirationTime = true,
                    ClockSkew = TimeSpan.Zero,
                    NameClaimType = ClaimsDoToken.Nome,
                    RoleClaimType = ClaimsDoToken.Perfil  // permite usar [Authorize(Roles = ...)] e RequireRole(...)
                };

                opcoes.Events = new JwtBearerEvents
                {
                    // Após validar assinatura/expiração, confere no banco se o usuário ainda existe e se o
                    // perfil do token continua igual. Assim, um usuário excluído ou que perdeu a permissão
                    // não consegue continuar usando um token antigo.
                    OnTokenValidated = async contexto =>
                    {
                        var banco = contexto.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                        var idDoToken = contexto.Principal?.FindFirstValue(ClaimsDoToken.Id);
                        var perfilDoToken = contexto.Principal?.FindFirstValue(ClaimsDoToken.Perfil);

                        var perfilAtual = int.TryParse(idDoToken, out var id)
                            ? await banco.Usuarios.Where(u => u.Id == id).Select(u => u.Perfil).FirstOrDefaultAsync()
                            : null;

                        if (perfilAtual is null)
                            contexto.Fail(new AuthenticationFailureException("O usuário do token não existe mais."));
                        else if (perfilAtual != perfilDoToken)
                            contexto.Fail(new AuthenticationFailureException(
                                "As permissões do usuário foram alteradas. Faça login novamente."));
                    },

                    // Respostas 401/403 padronizadas em JSON (application/problem+json).
                    OnChallenge = async contexto =>
                    {
                        contexto.HandleResponse(); // substitui a resposta padrão (corpo vazio)

                        var detalhe = contexto.AuthenticateFailure switch
                        {
                            SecurityTokenExpiredException => "O token de acesso expirou. Faça login novamente.",
                            AuthenticationFailureException falha => falha.Message,
                            not null => "O token de acesso é inválido.",
                            null => "Token de acesso não informado. Envie o cabeçalho 'Authorization: Bearer {token}'."
                        };

                        contexto.Response.Headers.WWWAuthenticate = contexto.AuthenticateFailure is null
                            ? "Bearer"
                            : "Bearer error=\"invalid_token\"";

                        await RespostaDeErro.EscreverAsync(contexto.HttpContext,
                            StatusCodes.Status401Unauthorized, "Não autenticado", detalhe);
                    },
                    OnForbidden = contexto => RespostaDeErro.EscreverAsync(contexto.HttpContext,
                        StatusCodes.Status403Forbidden, "Acesso negado",
                        "Você está autenticado, mas não possui permissão para executar esta operação.")
                };
            });

        // 3) AUTORIZAÇÃO baseada em perfis/permissões.
        services.AddAuthorizationBuilder()
            // Negação por padrão: qualquer endpoint sem [AllowAnonymous] exige usuário autenticado.
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
            .AddPolicy(Politicas.ExcluirRegistros, politica => politica
                .RequireAuthenticatedUser()
                .RequireRole(Perfis.Administrador))
            .AddPolicy(Politicas.GerenciarUsuarios, politica => politica
                .RequireAuthenticatedUser()
                .RequireRole(Perfis.Administrador));

        // 4) Limite de tentativas de login por IP (proteção contra ataques de força bruta).
        services.AddRateLimiter(opcoes =>
        {
            opcoes.AddPolicy(Politicas.LimiteDeLogin, contexto =>
            {
                var limitePorMinuto = contexto.RequestServices.GetRequiredService<IConfiguration>()
                    .GetValue("Seguranca:TentativasDeLoginPorMinuto", 5);

                return RateLimitPartition.GetFixedWindowLimiter(
                    contexto.Connection.RemoteIpAddress?.ToString() ?? "desconhecido",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = limitePorMinuto,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    });
            });

            opcoes.OnRejected = (contexto, _) => new ValueTask(RespostaDeErro.EscreverAsync(contexto.HttpContext,
                StatusCodes.Status429TooManyRequests, "Muitas tentativas",
                "Limite de tentativas de login excedido. Aguarde um minuto e tente novamente."));
        });

        return services;
    }
}
