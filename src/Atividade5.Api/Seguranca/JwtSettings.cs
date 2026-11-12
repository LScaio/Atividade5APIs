namespace Atividade5.Api.Seguranca;

/// <summary>Configurações do token JWT (seção "Jwt" do appsettings).</summary>
public class JwtSettings
{
    public const string Secao = "Jwt";

    /// <summary>Quem emitiu o token (claim "iss").</summary>
    public string Emissor { get; set; } = string.Empty;

    /// <summary>Para quem o token foi emitido (claim "aud").</summary>
    public string Audiencia { get; set; } = string.Empty;

    /// <summary>
    /// Chave secreta usada para assinar o token (HMAC-SHA256). Mínimo de 32 caracteres.
    /// Em produção deve vir de variável de ambiente ou cofre de segredos, nunca do código-fonte.
    /// </summary>
    public string Chave { get; set; } = string.Empty;

    public int ExpiracaoEmMinutos { get; set; } = 60;
}
