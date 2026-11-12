namespace Atividade5.Api.Seguranca;

/// <summary>
/// Nomes das políticas de autorização. Cada política representa uma PERMISSÃO;
/// a regra que define quem possui a permissão fica centralizada no Program/ConfiguracaoDeSeguranca.
/// </summary>
public static class Politicas
{
    /// <summary>Permissão para excluir registros (somente Administrador).</summary>
    public const string ExcluirRegistros = "ExcluirRegistros";

    /// <summary>Permissão para consultar usuários e alterar perfis (somente Administrador).</summary>
    public const string GerenciarUsuarios = "GerenciarUsuarios";

    /// <summary>Política de limite de requisições aplicada ao login (proteção contra força bruta).</summary>
    public const string LimiteDeLogin = "LimiteDeLogin";
}
