namespace Atividade5.Api.Models;

/// <summary>
/// Recurso protegido da API. Qualquer pessoa pode consultar,
/// mas somente usuários autenticados podem alterar e somente
/// quem possui permissão (perfil Administrador) pode excluir.
/// </summary>
public class Produto
{
    public int Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    public string? Descricao { get; set; }

    public decimal Preco { get; set; }

    public int Estoque { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public DateTime? AtualizadoEm { get; set; }
}
