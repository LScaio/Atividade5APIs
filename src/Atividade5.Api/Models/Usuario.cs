using Atividade5.Api.Seguranca;

namespace Atividade5.Api.Models;

/// <summary>
/// Usuário que pode se autenticar na API.
/// A senha NUNCA é armazenada em texto puro: guardamos apenas o hash (PBKDF2 + salt).
/// </summary>
public class Usuario
{
    public int Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string SenhaHash { get; set; } = string.Empty;

    /// <summary>Perfil (papel/role) usado nas regras de autorização.</summary>
    public string Perfil { get; set; } = Perfis.Usuario;

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}
