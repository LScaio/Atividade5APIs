using System.Security.Claims;
using Atividade5.Api.Data;
using Atividade5.Api.Dtos;
using Atividade5.Api.Models;
using Atividade5.Api.Seguranca;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Atividade5.Api.Controllers;

/// <summary>
/// CRUD de produtos com regras de acesso:
/// <list type="bullet">
/// <item>GET: público (não exige token);</item>
/// <item>POST/PUT: somente usuários autenticados;</item>
/// <item>DELETE: somente usuários autenticados que possuem a permissão "ExcluirRegistros" (perfil Administrador).</item>
/// </list>
/// </summary>
[ApiController]
[Route("api/produtos")]
[Produces("application/json")]
[Authorize] // por padrão, todas as ações do controller exigem autenticação
public class ProdutosController(AppDbContext contexto, ILogger<ProdutosController> logger) : ControllerBase
{
    /// <summary>Lista todos os produtos (acesso público).</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<Produto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Produto>>> Listar()
    {
        return Ok(await contexto.Produtos.AsNoTracking().OrderBy(p => p.Id).ToListAsync());
    }

    /// <summary>Busca um produto pelo id (acesso público).</summary>
    /// <response code="200">Produto encontrado.</response>
    /// <response code="404">Produto não encontrado.</response>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Produto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Produto>> Obter(int id)
    {
        var produto = await contexto.Produtos.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        return produto is null ? ProdutoNaoEncontrado(id) : Ok(produto);
    }

    /// <summary>Cadastra um produto (requer autenticação).</summary>
    /// <response code="201">Produto criado.</response>
    /// <response code="400">Dados inválidos.</response>
    /// <response code="401">Token ausente, inválido ou expirado.</response>
    [HttpPost]
    [ProducesResponseType(typeof(Produto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Produto>> Criar(ProdutoRequest requisicao)
    {
        var produto = new Produto
        {
            Nome = requisicao.Nome.Trim(),
            Descricao = requisicao.Descricao?.Trim(),
            Preco = requisicao.Preco,
            Estoque = requisicao.Estoque
        };

        contexto.Produtos.Add(produto);
        await contexto.SaveChangesAsync();

        logger.LogInformation("Produto {Id} criado por {Usuario}.", produto.Id, EmailDoUsuario());
        return CreatedAtAction(nameof(Obter), new { id = produto.Id }, produto);
    }

    /// <summary>Atualiza um produto (requer autenticação).</summary>
    /// <response code="200">Produto atualizado.</response>
    /// <response code="400">Dados inválidos.</response>
    /// <response code="401">Token ausente, inválido ou expirado.</response>
    /// <response code="404">Produto não encontrado.</response>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(Produto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Produto>> Atualizar(int id, ProdutoRequest requisicao)
    {
        var produto = await contexto.Produtos.FindAsync(id);
        if (produto is null)
            return ProdutoNaoEncontrado(id);

        produto.Nome = requisicao.Nome.Trim();
        produto.Descricao = requisicao.Descricao?.Trim();
        produto.Preco = requisicao.Preco;
        produto.Estoque = requisicao.Estoque;
        produto.AtualizadoEm = DateTime.UtcNow;

        await contexto.SaveChangesAsync();

        logger.LogInformation("Produto {Id} atualizado por {Usuario}.", id, EmailDoUsuario());
        return Ok(produto);
    }

    /// <summary>
    /// Exclui um produto. Somente usuários AUTENTICADOS e com PERMISSÃO de exclusão (perfil Administrador).
    /// </summary>
    /// <response code="204">Produto excluído.</response>
    /// <response code="401">Token ausente, inválido ou expirado (falha de autenticação).</response>
    /// <response code="403">Usuário autenticado, mas sem permissão para excluir (falha de autorização).</response>
    /// <response code="404">Produto não encontrado.</response>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = Politicas.ExcluirRegistros)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Excluir(int id)
    {
        var produto = await contexto.Produtos.FindAsync(id);
        if (produto is null)
            return ProdutoNaoEncontrado(id);

        contexto.Produtos.Remove(produto);
        await contexto.SaveChangesAsync();

        // Trilha de auditoria: quem excluiu o quê.
        logger.LogWarning("Produto {Id} ({Nome}) EXCLUÍDO por {Usuario}.", id, produto.Nome, EmailDoUsuario());
        return NoContent();
    }

    private ObjectResult ProdutoNaoEncontrado(int id) =>
        Problem(statusCode: StatusCodes.Status404NotFound,
            title: "Produto não encontrado", detail: $"Não existe produto com o id {id}.");

    private string? EmailDoUsuario() => User.FindFirstValue(ClaimsDoToken.Email);
}
