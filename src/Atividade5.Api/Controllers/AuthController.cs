using System.Security.Claims;
using Atividade5.Api.Data;
using Atividade5.Api.Dtos;
using Atividade5.Api.Models;
using Atividade5.Api.Seguranca;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Atividade5.Api.Controllers;

/// <summary>Cadastro de usuários e autenticação (emissão do token JWT).</summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController(
    AppDbContext contexto,
    IPasswordHasher<Usuario> hasher,
    ITokenService tokenService,
    ILogger<AuthController> logger) : ControllerBase
{
    /// <summary>Cadastra um novo usuário. Todo cadastro público recebe o perfil "Usuario".</summary>
    /// <response code="201">Usuário criado.</response>
    /// <response code="400">Dados inválidos.</response>
    /// <response code="409">E-mail já cadastrado.</response>
    [HttpPost("registrar")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(UsuarioResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UsuarioResponse>> Registrar(RegistrarUsuarioRequest requisicao)
    {
        var email = requisicao.Email.Trim().ToLowerInvariant();

        if (await contexto.Usuarios.AnyAsync(u => u.Email == email))
            return EmailJaCadastrado();

        // O perfil NÃO vem da requisição: impede que alguém se cadastre como Administrador.
        var usuario = new Usuario { Nome = requisicao.Nome.Trim(), Email = email, Perfil = Perfis.Usuario };
        usuario.SenhaHash = hasher.HashPassword(usuario, requisicao.Senha);

        contexto.Usuarios.Add(usuario);
        try
        {
            await contexto.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            // Dois cadastros simultâneos com o mesmo e-mail: o índice único do banco barra o segundo.
            logger.LogWarning(ex, "Falha ao gravar o usuário {Email}.", email);
            return EmailJaCadastrado();
        }

        logger.LogInformation("Usuário {Email} cadastrado com o perfil {Perfil}.", email, usuario.Perfil);
        return Created($"/api/usuarios/{usuario.Id}", UsuarioResponse.De(usuario));
    }

    /// <summary>Autentica o usuário e devolve um token JWT para ser enviado no cabeçalho Authorization.</summary>
    /// <response code="200">Login realizado; use o token retornado.</response>
    /// <response code="400">Dados inválidos.</response>
    /// <response code="401">E-mail ou senha incorretos.</response>
    /// <response code="429">Muitas tentativas de login em pouco tempo.</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(Politicas.LimiteDeLogin)]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<TokenResponse>> Login(LoginRequest requisicao)
    {
        var email = requisicao.Email.Trim().ToLowerInvariant();
        var usuario = await contexto.Usuarios.FirstOrDefaultAsync(u => u.Email == email);

        var resultado = usuario is null
            ? PasswordVerificationResult.Failed
            : hasher.VerifyHashedPassword(usuario, usuario.SenhaHash, requisicao.Senha);

        if (usuario is null || resultado == PasswordVerificationResult.Failed)
        {
            logger.LogWarning("Login inválido para {Email} (IP {Ip}).", email, HttpContext.Connection.RemoteIpAddress);

            // Mensagem genérica: não revela se o e-mail existe ou se apenas a senha está errada.
            return Problem(statusCode: StatusCodes.Status401Unauthorized,
                title: "Credenciais inválidas", detail: "E-mail ou senha incorretos.");
        }

        if (resultado == PasswordVerificationResult.SuccessRehashNeeded)
        {
            usuario.SenhaHash = hasher.HashPassword(usuario, requisicao.Senha);
            await contexto.SaveChangesAsync();
        }

        logger.LogInformation("Usuário {Email} autenticado.", email);
        return Ok(tokenService.GerarToken(usuario));
    }

    /// <summary>Retorna os dados do usuário dono do token (útil para depurar a autenticação).</summary>
    /// <response code="200">Dados do usuário autenticado.</response>
    /// <response code="401">Token ausente, inválido ou expirado.</response>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UsuarioResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public ActionResult<UsuarioResponse> UsuarioAtual()
    {
        return Ok(new UsuarioResponse(
            Id: int.Parse(User.FindFirstValue(ClaimsDoToken.Id)!),
            Nome: User.Identity!.Name!,
            Email: User.FindFirstValue(ClaimsDoToken.Email)!,
            Perfil: User.FindFirstValue(ClaimsDoToken.Perfil)!));
    }

    private ObjectResult EmailJaCadastrado() =>
        Problem(statusCode: StatusCodes.Status409Conflict,
            title: "E-mail já cadastrado", detail: "Já existe um usuário cadastrado com este e-mail.");
}
