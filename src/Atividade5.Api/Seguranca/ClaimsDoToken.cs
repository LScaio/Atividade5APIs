namespace Atividade5.Api.Seguranca;

/// <summary>Nomes das claims gravadas no token JWT.</summary>
public static class ClaimsDoToken
{
    public const string Id = "sub";
    public const string Nome = "name";
    public const string Email = "email";
    public const string Perfil = "role";
}
