using System.ComponentModel.DataAnnotations;

namespace Atividade5.Api.Dtos;

/// <summary>Dados para criar/atualizar um produto.</summary>
public class ProdutoRequest
{
    [Required(ErrorMessage = "O nome é obrigatório.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "O nome deve ter entre 2 e 100 caracteres.")]
    public string Nome { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "A descrição deve ter no máximo 500 caracteres.")]
    public string? Descricao { get; set; }

    [Range(typeof(decimal), "0.01", "1000000", ErrorMessage = "O preço deve estar entre 0,01 e 1.000.000.")]
    public decimal Preco { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "O estoque não pode ser negativo.")]
    public int Estoque { get; set; }
}
