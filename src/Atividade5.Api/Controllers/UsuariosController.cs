using System.Security.Claims;
using Atividade5.Api.Data;
using Atividade5.Api.Dtos;
using Atividade5.Api.Seguranca;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Atividade5.Api.Controllers;

/// <summary>Gestão de usuários e de permissões (somente Administrador).</summary>
[ApiController]
[Route("api/usuarios")]
[Produces("application/json")]
[Authorize(Policy = Politicas.GerenciarUsuarios)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
public class UsuariosController(AppDbContext contexto, ILogger<UsuariosController> logger) : ControllerBase
{
    /// <summary>Lista os usuários cadastrados (sem expor o hash da senha).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<UsuarioResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<UsuarioResponse>>> Listar()
    {
        var usuarios = await contexto.Usuarios.AsNoTracking()
            .OrderBy(u => u.Id)
            .Select(u => new UsuarioResponse(u.Id, u.Nome, u.Email, u.Perfil))
            .ToListAsync();

        return Ok(usuarios);
    }

    /// <summary>Busca um usuário pelo id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(UsuarioResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UsuarioResponse>> Obter(int id)
    {
        var usuario = await contexto.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
        return usuario is null ? UsuarioNaoEncontrado(id) : Ok(UsuarioResponse.De(usuario));
    }

    /// <summary>
    /// Concede ou remove permissões alterando o perfil do usuário ("Administrador" ou "Usuario").
    /// Tokens emitidos antes da alteração deixam de ser aceitos (o usuário precisa fazer login novamente).
    /// </summary>
    /// <response code="200">Perfil alterado.</response>
    /// <response code="400">Perfil inválido ou tentativa de alterar o próprio perfil.</response>
    /// <response code="404">Usuário não encontrado.</response>
    [HttpPut("{id:int}/perfil")]
    [ProducesResponseType(typeof(UsuarioResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UsuarioResponse>> AlterarPerfil(int id, AlterarPerfilRequest requisicao)
    {
        if (User.FindFirstValue(ClaimsDoToken.Id) == id.ToString())
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Operação não permitida",
                detail: "Você não pode alterar o seu próprio perfil.");

        var usuario = await contexto.Usuarios.FindAsync(id);
        if (usuario is null)
            return UsuarioNaoEncontrado(id);

        var perfilAnterior = usuario.Perfil;
        usuario.Perfil = requisicao.Perfil;
        await contexto.SaveChangesAsync();

        logger.LogWarning("Perfil do usuário {Email} alterado de {De} para {Para} por {Admin}.",
            usuario.Email, perfilAnterior, usuario.Perfil, User.FindFirstValue(ClaimsDoToken.Email));

        return Ok(UsuarioResponse.De(usuario));
    }

    private ObjectResult UsuarioNaoEncontrado(int id) =>
        Problem(statusCode: StatusCodes.Status404NotFound,
            title: "Usuário não encontrado", detail: $"Não existe usuário com o id {id}.");
}
