using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Atividade5.Api.Dtos;
using Atividade5.Api.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Atividade5.Api.Seguranca;

public interface ITokenService
{
    TokenResponse GerarToken(Usuario usuario);
}

/// <summary>Gera tokens JWT assinados para usuários autenticados.</summary>
public class TokenService(IOptions<JwtSettings> configuracao) : ITokenService
{
    public TokenResponse GerarToken(Usuario usuario)
    {
        var jwt = configuracao.Value;
        var agora = DateTime.UtcNow;
        var expiraEm = agora.AddMinutes(jwt.ExpiracaoEmMinutos);

        // Claims: informações sobre o usuário que viajam dentro do token.
        // Nunca coloque dados sensíveis (ex.: senha) no token: ele é apenas codificado, não criptografado.
        var claims = new[]
        {
            new Claim(ClaimsDoToken.Id, usuario.Id.ToString()),
            new Claim(ClaimsDoToken.Nome, usuario.Nome),
            new Claim(ClaimsDoToken.Email, usuario.Email),
            new Claim(ClaimsDoToken.Perfil, usuario.Perfil),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, EpochTime.GetIntDate(agora).ToString(), ClaimValueTypes.Integer64)
        };

        var chave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Chave));
        var credenciais = new SigningCredentials(chave, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwt.Emissor,
            audience: jwt.Audiencia,
            claims: claims,
            notBefore: agora,
            expires: expiraEm,
            signingCredentials: credenciais);

        return new TokenResponse(
            Token: new JwtSecurityTokenHandler().WriteToken(token),
            Tipo: "Bearer",
            ExpiraEm: expiraEm,
            Usuario: UsuarioResponse.De(usuario));
    }
}
