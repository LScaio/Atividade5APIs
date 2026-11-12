using Atividade5.Api.Models;
using Atividade5.Api.Seguranca;
using Microsoft.EntityFrameworkCore;

namespace Atividade5.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Produto> Produtos => Set<Produto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>(entidade =>
        {
            entidade.ToTable("Usuarios");
            entidade.Property(u => u.Nome).HasMaxLength(100).IsRequired();
            entidade.Property(u => u.Email).HasMaxLength(150).IsRequired();
            entidade.HasIndex(u => u.Email).IsUnique(); // não permite dois usuários com o mesmo e-mail
            entidade.Property(u => u.SenhaHash).HasMaxLength(256).IsRequired();
            entidade.Property(u => u.Perfil).HasMaxLength(30).IsRequired().HasDefaultValue(Perfis.Usuario);
        });

        modelBuilder.Entity<Produto>(entidade =>
        {
            entidade.ToTable("Produtos");
            entidade.Property(p => p.Nome).HasMaxLength(100).IsRequired();
            entidade.Property(p => p.Descricao).HasMaxLength(500);
            entidade.Property(p => p.Preco).HasPrecision(18, 2);

            // Dados iniciais para facilitar os testes.
            var dataCadastro = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            entidade.HasData(
                new Produto { Id = 1, Nome = "Notebook", Descricao = "Notebook 15,6\" 16 GB RAM", Preco = 4599.90m, Estoque = 10, CriadoEm = dataCadastro },
                new Produto { Id = 2, Nome = "Mouse sem fio", Descricao = "Mouse óptico 1600 DPI", Preco = 89.90m, Estoque = 50, CriadoEm = dataCadastro },
                new Produto { Id = 3, Nome = "Teclado mecânico", Descricao = "Teclado ABNT2 switch blue", Preco = 299.00m, Estoque = 25, CriadoEm = dataCadastro });
        });
    }
}
