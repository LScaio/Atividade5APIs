using System.ComponentModel.DataAnnotations;
using Atividade5.Api.Models;

namespace Atividade5.Api.Dtos;

/// <summary>Dados para cadastro de um novo usuário (sempre criado com o perfil "Usuario").</summary>
public class RegistrarUsuarioRequest
{
    [Required(ErrorMessage = "O nome é obrigatório.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "O nome deve ter entre 3 e 100 caracteres.")]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "O e-mail é obrigatório.")]
    [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
    [StringLength(150, ErrorMessage = "O e-mail deve ter no máximo 150 caracteres.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "A senha é obrigatória.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "A senha deve ter entre 8 e 100 caracteres.")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$",
        ErrorMessage = "A senha deve conter ao menos uma letra maiúscula, uma minúscula e um número.")]
    public string Senha { get; set; } = string.Empty;
}

/// <summary>Credenciais de acesso.</summary>
public class LoginRequest
{
    [Required(ErrorMessage = "O e-mail é obrigatório.")]
    [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "A senha é obrigatória.")]
    public string Senha { get; set; } = string.Empty;
}

/// <summary>Token devolvido após um login bem-sucedido.</summary>
public record TokenResponse(string Token, string Tipo, DateTime ExpiraEm, UsuarioResponse Usuario);

/// <summary>Dados públicos do usuário (sem a senha/hash).</summary>
public record UsuarioResponse(int Id, string Nome, string Email, string Perfil)
{
    public static UsuarioResponse De(Usuario usuario) => new(usuario.Id, usuario.Nome, usuario.Email, usuario.Perfil);
}

/// <summary>Alteração do perfil (permissão) de um usuário.</summary>
public class AlterarPerfilRequest
{
    [Required(ErrorMessage = "O perfil é obrigatório.")]
    [RegularExpression("^(Administrador|Usuario)$", ErrorMessage = "Perfil inválido. Use 'Administrador' ou 'Usuario'.")]
    public string Perfil { get; set; } = string.Empty;
}
