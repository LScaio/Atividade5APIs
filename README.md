# Atividade 5 – API com Autenticação e Autorização

API REST em **ASP.NET Core 8 (C#)** com **Entity Framework Core + SQL Server**. Ela restringe o acesso aos recursos para que **somente usuários autenticados, e com permissão, consigam excluir registros**.

> **Autenticação** verifica *quem é* o usuário (login → token JWT).
> **Autorização** verifica *o que* ele pode fazer (perfil/permissão exigidos por endpoint).

---

## 1. Regras de acesso implementadas

| Endpoint | Método | Quem pode acessar | Sem token | Usuário comum | Administrador |
|---|---|---|---|---|---|
| `/api/auth/registrar` | POST | Público | 201 | – | – |
| `/api/auth/login` | POST | Público (máx. 5 tentativas/min por IP) | 200 / 401 | – | – |
| `/api/auth/me` | GET | Autenticado | **401** | 200 | 200 |
| `/api/produtos` e `/api/produtos/{id}` | GET | Público | 200 | 200 | 200 |
| `/api/produtos` | POST | Autenticado | **401** | 201 | 201 |
| `/api/produtos/{id}` | PUT | Autenticado | **401** | 200 | 200 |
| **`/api/produtos/{id}`** | **DELETE** | **Autenticado + permissão `ExcluirRegistros`** | **401** | **403** | **204** |
| `/api/usuarios` e `/api/usuarios/{id}` | GET | Permissão `GerenciarUsuarios` | **401** | **403** | 200 |
| `/api/usuarios/{id}/perfil` | PUT | Permissão `GerenciarUsuarios` | **401** | **403** | 200 |

* **401 Unauthorized**: o usuário não está autenticado (token ausente, inválido, adulterado ou expirado).
* **403 Forbidden**: o usuário está autenticado, mas **não tem permissão** para a operação.

---

## 2. Recursos de segurança aplicados

| Recurso | Onde está | Descrição |
|---|---|---|
| Autenticação JWT (Bearer) | `Seguranca/ConfiguracaoDeSeguranca.cs`, `Seguranca/TokenService.cs` | O login gera um token assinado com HMAC-SHA256. A API valida assinatura, emissor, audiência, algoritmo e expiração (60 min). |
| Autorização por políticas/perfis | `Seguranca/Politicas.cs`, `[Authorize(Policy = Politicas.ExcluirRegistros)]` no `ProdutosController` | O DELETE exige a permissão `ExcluirRegistros`, que hoje só o perfil **Administrador** tem. |
| Negação por padrão | `SetFallbackPolicy(...)` | Qualquer endpoint novo exige autenticação, a menos que esteja marcado com `[AllowAnonymous]`. |
| Revalidação do usuário a cada requisição | `OnTokenValidated` | Se o usuário for excluído ou mudar de perfil, o token antigo deixa de valer na hora. Não é preciso esperar ele expirar. |
| Senhas com hash + salt | `PasswordHasher<Usuario>` (PBKDF2-HMAC-SHA512, 100.000 iterações) | A senha nunca é gravada em texto puro e nunca aparece nas respostas. |
| Política de senha forte | `Dtos/AuthDtos.cs` | Mínimo de 8 caracteres, com maiúscula, minúscula e número. |
| Proteção contra *mass assignment* | DTOs de entrada | No cadastro público, o perfil é sempre `Usuario`. Não dá para se cadastrar como Administrador. |
| Mensagem genérica no login | `AuthController.Login` | A resposta "E-mail ou senha incorretos" não revela se o e-mail existe. |
| Limite de tentativas de login | `AddRateLimiter` | Retorna **429** após 5 tentativas por minuto por IP (protege contra força bruta). |
| E-mail único | Índice único no banco + tratamento de `DbUpdateException` | Um e-mail repetido retorna **409**. |
| Segredos fora do código de produção | `appsettings.json` (chave vazia) | Fora do ambiente de desenvolvimento, a API **não inicia** sem uma chave JWT de 32 caracteres ou mais. |
| Tratamento global de exceções | `Erros/TratadorGlobalDeExcecoes.cs` | Registra o erro no log e devolve um JSON padronizado sem stack trace (detalhes só em desenvolvimento). |
| Auditoria | `ILogger` nos controllers | Registra quem criou, alterou ou **excluiu** cada registro, além das tentativas de login inválidas. |
| HTTPS / HSTS | `Program.cs` | Redireciona para HTTPS e usa HSTS em produção. |

Todos os erros seguem o padrão **RFC 9457 (`application/problem+json`)**:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.4",
  "title": "Acesso negado",
  "status": 403,
  "detail": "Você está autenticado, mas não possui permissão para executar esta operação.",
  "instance": "/api/produtos/4",
  "traceId": "00-c3feaef02292a3485bfa4d1558aa3299-87d57c97f0c83266-00"
}
```

---

## 3. Como executar

### Pré-requisitos
* .NET 8 SDK (Visual Studio 2022 ou VS Code)
* SQL Server (Express, Developer ou LocalDB) + SSMS / Azure Data Studio
* Postman ou Insomnia

### Banco de dados
A connection string fica em `src/Atividade5.Api/appsettings.json`:

```json
"ConexaoPadrao": "Server=localhost;Database=Atividade5ApiDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
```

Ajuste o `Server` de acordo com a sua instalação:

| Instalação | Valor de `Server` (no JSON a barra `\` é escrita duplicada) |
|---|---|
| SQL Server Developer (instância padrão) | `localhost` |
| SQL Server Express | `localhost\\SQLEXPRESS` |
| LocalDB (Visual Studio) | `(localdb)\\mssqllocaldb` |

**Não é preciso criar o banco manualmente.** Ao iniciar, a API aplica as *migrations* (cria o banco `Atividade5ApiDb`, as tabelas `Usuarios` e `Produtos`, insere 3 produtos de exemplo) e cadastra o administrador padrão.
Se preferir criar pelo SSMS, use `docs/banco-de-dados.sql`.

### Executar
**Visual Studio:** abra `Atividade5APIs.sln`, defina `Atividade5.Api` como projeto de inicialização e pressione **F5**. O Swagger abre automaticamente.

**Linha de comando:**

```bash
dotnet run --project src/Atividade5.Api --launch-profile https
```

* Swagger: `https://localhost:7080/swagger` (ou `http://localhost:5080/swagger`)
* Na primeira execução com HTTPS, confie no certificado de desenvolvimento com `dotnet dev-certs https --trust`.

### Usuários para teste

| Perfil | E-mail | Senha |
|---|---|---|
| Administrador (criado automaticamente) | `admin@atividade5.com` | `Admin@123` |
| Usuário comum | cadastre em `POST /api/auth/registrar` | – |

> A chave JWT e o administrador padrão estão em `appsettings.Development.json` **somente para desenvolvimento**. Em produção, informe a chave por variável de ambiente (`Jwt__Chave`) ou pelo `dotnet user-secrets`.

---

## 4. Como testar

### Postman / Insomnia
1. Importe `docs/Atividade5.postman_collection.json`. O Insomnia também importa coleções do Postman.
2. Execute as pastas **em ordem** ou use o *Collection Runner*. Os tokens são salvos automaticamente nas variáveis da coleção.
3. Se a API estiver rodando em outra porta, altere a variável `baseUrl`.

| Pasta | O que demonstra |
|---|---|
| 0 – Acesso público | GET sem token → 200 |
| 1 – Autenticação | cadastro (201), login (200), senha errada (401), `/me` (200) |
| 2 – Produtos | POST sem token (401), POST/PUT com token (201/200), dados inválidos (400) |
| **3 – Exclusão** | **sem token → 401, token inválido → 401, usuário comum → 403, administrador → 204**, produto excluído → 404 |
| 4 – Permissões | usuário comum não lista usuários (403), admin promove usuário (200), token antigo deixa de valer (401) |

> Para testar manualmente, envie o cabeçalho `Authorization: Bearer {token}`. No Postman, use a aba **Authorization → Bearer Token**.
> O login aceita 5 tentativas por minuto. Se aparecer **429**, aguarde 1 minuto.

### Swagger
1. Execute `POST /api/auth/login` e copie o valor de `token`.
2. Clique em **Authorize** e cole o token, **sem** a palavra `Bearer`.
3. Teste o `DELETE /api/produtos/{id}` com o token do administrador e com o de um usuário comum.

### Visual Studio (`.http`)
O arquivo `src/Atividade5.Api/Atividade5.Api.http` traz o mesmo roteiro, pronto para executar no Visual Studio 2022.

### Testes automatizados (xUnit)

```bash
dotnet test
```

São 23 testes de integração. Eles sobem a API em memória e verificam, entre outros pontos:
* DELETE sem token, com token malformado, **expirado** ou **forjado com outra chave** → 401;
* DELETE com usuário sem permissão → 403 (e o produto continua no banco);
* DELETE com administrador → 204 (e o produto deixa de existir);
* usuário promovido ou rebaixado → o token antigo é invalidado;
* a senha é gravada apenas como hash e nunca volta na resposta;
* não é possível se cadastrar como Administrador;
* limite de tentativas de login → 429.

---

## 5. Códigos de status utilizados

| Código | Quando ocorre |
|---|---|
| 200 OK | Consulta, login ou atualização bem-sucedidos |
| 201 Created | Usuário ou produto criado (com cabeçalho `Location`) |
| 204 No Content | Produto excluído |
| 400 Bad Request | Dados inválidos (o JSON lista os erros por campo) |
| 401 Unauthorized | Não autenticado: token ausente, inválido, expirado ou credenciais incorretas |
| 403 Forbidden | Autenticado, mas sem permissão |
| 404 Not Found | Recurso inexistente |
| 409 Conflict | E-mail já cadastrado |
| 429 Too Many Requests | Excesso de tentativas de login |
| 500 Internal Server Error | Erro inesperado (registrado no log, resposta sem detalhes internos) |

---

## 6. Estrutura do projeto

```
Atividade5APIs.sln
├── src/Atividade5.Api
│   ├── Controllers/        AuthController, ProdutosController, UsuariosController
│   ├── Data/               AppDbContext, DbInicializador, Migrations/
│   ├── Dtos/               Modelos de entrada/saída (validações com DataAnnotations)
│   ├── Erros/              Tratador global de exceções e respostas problem+json
│   ├── Models/             Usuario, Produto
│   ├── Seguranca/          JWT, políticas, perfis, TokenService, configuração de segurança
│   ├── Program.cs          Pipeline: ExceptionHandler → RateLimiter → Authentication → Authorization
│   └── Atividade5.Api.http Roteiro de testes do Visual Studio
├── tests/Atividade5.Api.Tests  Testes de integração (xUnit + WebApplicationFactory)
└── docs/
    ├── Atividade5.postman_collection.json
    └── banco-de-dados.sql
```
