namespace Atividade5.Api.Seguranca;

/// <summary>Perfis (roles) existentes na aplicação.</summary>
public static class Perfis
{
    public const string Administrador = "Administrador";
    public const string Usuario = "Usuario";

    public static readonly string[] Todos = [Administrador, Usuario];
}
