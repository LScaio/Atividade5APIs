using System.Reflection;
using Atividade5.Api.Data;
using Atividade5.Api.Erros;
using Atividade5.Api.Seguranca;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ---------- Banco de dados (SQL Server via Entity Framework Core) ----------
builder.Services.AddDbContext<AppDbContext>((services, options) =>
{
    var configuracao = services.GetRequiredService<IConfiguration>();

    // "InMemory" é útil apenas para testes automatizados ou para rodar sem SQL Server instalado.
    if (configuracao["BancoDeDados:Provedor"] == "InMemory")
        options.UseInMemoryDatabase(configuracao["BancoDeDados:NomeEmMemoria"] ?? "Atividade5Api");
    else
        options.UseSqlServer(configuracao.GetConnectionString("ConexaoPadrao")
            ?? throw new InvalidOperationException("Connection string 'ConexaoPadrao' não configurada."));
});

// ---------- Autenticação (JWT), autorização (políticas/perfis) e limite de tentativas de login ----------
builder.Services.AddSegurancaDaApi(builder.Configuration);

// ---------- Tratamento global de exceções ----------
builder.Services.AddExceptionHandler<TratadorGlobalDeExcecoes>();
builder.Services.AddProblemDetails();

// ---------- Controllers + resposta padronizada para erros de validação (400) ----------
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(opcoes =>
    {
        opcoes.InvalidModelStateResponseFactory = contexto =>
        {
            var problema = contexto.HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>()
                .CreateValidationProblemDetails(contexto.HttpContext, contexto.ModelState,
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Dados inválidos",
                    detail: "Corrija os campos indicados em 'errors' e tente novamente.",
                    instance: contexto.HttpContext.Request.Path);

            return new BadRequestObjectResult(problema) { ContentTypes = { "application/problem+json" } };
        };
    });

// ---------- Swagger com suporte a token JWT (botão "Authorize") ----------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Atividade 5 - API com Autenticação e Autorização",
        Version = "v1",
        Description = "1) Faça login em POST /api/auth/login; 2) clique em 'Authorize' e cole o token; " +
                      "3) somente usuários com perfil Administrador podem usar DELETE."
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Cole apenas o token JWT (sem a palavra 'Bearer')."
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });

    var arquivoXml = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
    if (File.Exists(arquivoXml))
        c.IncludeXmlComments(arquivoXml);
});

var app = builder.Build();

// Cria/atualiza o banco e cadastra o administrador inicial.
using (var escopo = app.Services.CreateScope())
{
    await DbInicializador.InicializarAsync(escopo.ServiceProvider);
}

// ---------- Pipeline HTTP (a ordem importa!) ----------
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseAuthentication();   // 1º identifica QUEM é o usuário (valida o token)
app.UseAuthorization();    // 2º verifica O QUE ele pode fazer (políticas/perfis)

app.MapControllers();

app.Run();

/// <summary>Necessário para os testes de integração (WebApplicationFactory).</summary>
public partial class Program;
