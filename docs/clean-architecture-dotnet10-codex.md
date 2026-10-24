# Guia de Implementação — Clean Architecture em .NET 10

> **Objetivo:** usar este documento como instrução para o Codex implementar ou adaptar uma solução .NET 10 seguindo Clean Architecture, respeitando o código e as convenções já existentes no repositório.

## 1. Instruções gerais para o Codex

Antes de alterar arquivos:

1. Inspecione a solução existente (`.sln`/`.slnx`), os projetos, os pacotes NuGet, a configuração, os testes e as convenções já adotadas.
2. Identifique se o repositório já possui uma arquitetura estabelecida. **Prefira adaptar a estrutura existente a recriar tudo do zero.**
3. Confirme que os projetos usam .NET 10 (`net10.0`) quando isso fizer parte do escopo. Não atualize dependências ou frameworks sem necessidade.
4. Apresente um plano breve de implementação antes de mudanças amplas.
5. Implemente em etapas pequenas e coerentes. Evite alterações não relacionadas ao objetivo.
6. Execute `dotnet build` e os testes relevantes ao final. Corrija os problemas introduzidos pelas alterações.
7. Não declare que algo foi testado se o comando não tiver sido executado com sucesso.

Se o repositório estiver vazio, use a estrutura e as decisões recomendadas abaixo. Se houver requisitos explícitos no projeto que conflitem com este guia, priorize os requisitos do projeto e documente a decisão.

## 2. Princípios arquiteturais

Implemente os seguintes princípios:

- **Regra de dependência:** as dependências de código apontam para dentro, em direção ao domínio.
- **Separação de responsabilidades:** cada camada possui uma finalidade clara.
- **Domínio independente:** regras de negócio não dependem de ASP.NET Core, Entity Framework Core, banco de dados ou serviços externos.
- **Casos de uso explícitos:** operações da aplicação ficam em handlers ou serviços de aplicação, e não em controllers/endpoints.
- **Testabilidade:** regras e casos de uso devem poder ser testados sem iniciar a API ou depender de infraestrutura real.
- **Inversão de dependência:** a Application define abstrações necessárias aos casos de uso; Infrastructure implementa as abstrações.
- **Simplicidade:** não introduza abstrações, bibliotecas ou camadas sem uma necessidade concreta.

## 3. Camadas e responsabilidades

### 3.1 Domain

Contém o modelo e as regras essenciais do negócio.

Pode incluir:
- Entidades e agregados.
- Value Objects.
- Enums de domínio.
- Exceções e regras de domínio.
- Eventos de domínio, se necessários.

Regras:
- Não referenciar Application, Infrastructure, API ou frameworks externos.
- Não colocar DTOs de API, configurações de banco ou lógica de transporte nesta camada.
- Encapsular invariantes de negócio em vez de depender exclusivamente de validação na API.
- Evitar entidades anêmicas quando existirem regras de negócio relevantes.

Exemplo ilustrativo:

```csharp
namespace MyApp.Domain.Products;

public sealed class Product
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public decimal Price { get; private set; }

    private Product() { } // Pode ser útil para materialização do ORM.

    public Product(Guid id, string name, decimal price)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Product ID is required.", nameof(id));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name is required.", nameof(name));

        if (price < 0)
            throw new ArgumentOutOfRangeException(nameof(price));

        Id = id;
        Name = name.Trim();
        Price = price;
    }

    public void ChangePrice(decimal price)
    {
        if (price < 0)
            throw new ArgumentOutOfRangeException(nameof(price));

        Price = price;
    }
}
```

Adapte construtores, visibilidade e validações às necessidades reais do ORM e do domínio. Não copie o exemplo cegamente.

### 3.2 Application

Orquestra os casos de uso do sistema.

Pode incluir:
- Commands/Queries e handlers, ou serviços de aplicação equivalentes.
- DTOs de entrada e saída próprios dos casos de uso.
- Interfaces de repositórios e de serviços externos necessários à aplicação.
- Abstrações de transação, relógio, identidade do usuário ou publicação de eventos, quando necessárias.
- Validações de entrada e resultados dos casos de uso.

Regras:
- Pode referenciar Domain.
- Não deve depender de Infrastructure, da API ou de implementações concretas de banco de dados.
- Não expor entidades de persistência como contratos públicos da API.
- Manter casos de uso pequenos, explícitos e testáveis.
- Não adicionar MediatR, AutoMapper, FluentValidation ou bibliotecas semelhantes sem necessidade e sem verificar a compatibilidade com o projeto.

Exemplo de contrato de repositório:

```csharp
using MyApp.Domain.Products;

namespace MyApp.Application.Products;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Product product,
        CancellationToken cancellationToken = default);
}
```

Exemplo de caso de uso simples:

```csharp
using MyApp.Domain.Products;

namespace MyApp.Application.Products;

public sealed class GetProductById
{
    private readonly IProductRepository _products;

    public GetProductById(IProductRepository products)
    {
        _products = products;
    }

    public Task<Product?> ExecuteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _products.GetByIdAsync(id, cancellationToken);
    }
}
```

Este exemplo é ilustrativo. Siga o padrão de casos de uso já utilizado no repositório, se houver.

### 3.3 Infrastructure

Implementa integrações e detalhes técnicos.

Pode incluir:
- `DbContext` e configurações do Entity Framework Core.
- Implementações de repositórios.
- Migrações de banco de dados.
- Clientes HTTP para APIs externas.
- Armazenamento de arquivos, mensageria, e-mail e outros adaptadores externos.
- Registro de dependências de infraestrutura.

Regras:
- Pode referenciar Application e Domain.
- Implementa interfaces definidas pela Application (ou pelo Domain quando apropriado).
- Não deve conter regras de negócio que pertençam ao Domain.
- Não deve obrigar a Application a conhecer EF Core, SQL Server ou outro provedor.
- Segredos e connection strings não devem ser escritos diretamente no código-fonte.

Exemplo de implementação:

```csharp
using Microsoft.EntityFrameworkCore;
using MyApp.Application.Products;
using MyApp.Domain.Products;

namespace MyApp.Infrastructure.Persistence.Repositories;

internal sealed class ProductRepository : IProductRepository
{
    private readonly AppDbContext _dbContext;

    public ProductRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Product?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Products
            .SingleOrDefaultAsync(
                product => product.Id == id,
                cancellationToken);
    }

    public async Task AddAsync(
        Product product,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Products.AddAsync(product, cancellationToken);
    }
}
```

A persistência das alterações deve seguir uma estratégia consistente. Se o projeto usar Unit of Work, transações ou `SaveChangesAsync` nos handlers, preserve esse padrão e evite salvar implicitamente em vários lugares sem uma decisão arquitetural clara.

### 3.4 Presentation / API

É o ponto de entrada HTTP da aplicação.

Pode incluir:
- Controllers ou Minimal APIs.
- Modelos específicos de request/response HTTP.
- Autenticação e autorização.
- Middleware e filtros.
- Configuração de serialização e versionamento, se necessário.
- Tratamento consistente de erros HTTP e documentação OpenAPI.

Regras:
- A API chama casos de uso da Application.
- Não acessa `DbContext` nem repositórios concretos diretamente.
- Não contém regras centrais de negócio.
- Valida formato e requisitos do protocolo HTTP; regras de negócio devem também ser protegidas na camada adequada.
- Respeita códigos HTTP, cancelamento de requisições e contratos de resposta consistentes.

Exemplo ilustrativo de Minimal API:

```csharp
app.MapGet(
    "/products/{id:guid}",
    async (
        Guid id,
        GetProductById useCase,
        CancellationToken cancellationToken) =>
    {
        var product = await useCase.ExecuteAsync(id, cancellationToken);

        return product is null
            ? Results.NotFound()
            : Results.Ok(new
            {
                product.Id,
                product.Name,
                product.Price
            });
    });
```

Para uma API maior, considere separar os endpoints por feature e mapear explicitamente DTOs de resposta. Não retorne entidades de domínio diretamente por padrão.

## 4. Estrutura sugerida da solução

Adapte o nome `MyApp` ao nome real do sistema.

```text
src/
├── MyApp.Domain/
│   ├── Products/
│   │   ├── Product.cs
│   │   └── ProductRules.cs
│   └── MyApp.Domain.csproj
├── MyApp.Application/
│   ├── Products/
│   │   ├── IProductRepository.cs
│   │   └── GetProductById.cs
│   └── MyApp.Application.csproj
├── MyApp.Infrastructure/
│   ├── Persistence/
│   │   ├── AppDbContext.cs
│   │   └── Repositories/
│   ├── DependencyInjection.cs
│   └── MyApp.Infrastructure.csproj
└── MyApp.Api/
    ├── Endpoints/
    ├── Middleware/
    ├── Program.cs
    └── MyApp.Api.csproj

tests/
├── MyApp.Domain.Tests/
├── MyApp.Application.Tests/
└── MyApp.Api.IntegrationTests/
```

Não crie uma camada `Shared` automaticamente. Só a adicione se houver conceitos realmente compartilhados e bem definidos; evite transformá-la em um local genérico para classes sem responsabilidade clara.

## 5. Referências entre projetos

As referências esperadas são:

```text
Domain          → nenhuma camada da solução
Application     → Domain
Infrastructure  → Application + Domain
Api             → Application + Infrastructure
Domain.Tests    → Domain
Application.Tests → Application (+ Domain quando necessário)
Api.IntegrationTests → Api e dependências necessárias para testes
```

A API pode referenciar Infrastructure no composition root para registrar as implementações. O código dos endpoints deve depender de abstrações e casos de uso, não de classes concretas de persistência.

Não adicione referências desnecessárias entre projetos. Verifique os arquivos `.csproj` após as alterações.

## 6. Criação da solução (somente se o repositório estiver vazio)

Verifique primeiro a versão do SDK:

```bash
dotnet --list-sdks
```

Se o SDK .NET 10 estiver disponível e a solução realmente precisar ser criada do zero, use comandos equivalentes a:

```bash
dotnet new sln -n MyApp

dotnet new classlib -n MyApp.Domain -o src/MyApp.Domain -f net10.0
dotnet new classlib -n MyApp.Application -o src/MyApp.Application -f net10.0
dotnet new classlib -n MyApp.Infrastructure -o src/MyApp.Infrastructure -f net10.0
dotnet new webapi -n MyApp.Api -o src/MyApp.Api -f net10.0

dotnet sln add src/MyApp.Domain/MyApp.Domain.csproj
dotnet sln add src/MyApp.Application/MyApp.Application.csproj
dotnet sln add src/MyApp.Infrastructure/MyApp.Infrastructure.csproj
dotnet sln add src/MyApp.Api/MyApp.Api.csproj

dotnet add src/MyApp.Application/MyApp.Application.csproj reference src/MyApp.Domain/MyApp.Domain.csproj
dotnet add src/MyApp.Infrastructure/MyApp.Infrastructure.csproj reference src/MyApp.Application/MyApp.Application.csproj
dotnet add src/MyApp.Infrastructure/MyApp.Infrastructure.csproj reference src/MyApp.Domain/MyApp.Domain.csproj
dotnet add src/MyApp.Api/MyApp.Api.csproj reference src/MyApp.Application/MyApp.Application.csproj
dotnet add src/MyApp.Api/MyApp.Api.csproj reference src/MyApp.Infrastructure/MyApp.Infrastructure.csproj
```

Os comandos de criação podem variar conforme o SDK instalado. Confirme a sintaxe suportada pela versão local antes de executar. Não substitua nem sobrescreva uma solução existente sem autorização.

## 7. Injeção de dependência

Registre as dependências nos pontos de composição adequados.

- A Application pode oferecer um método de extensão `AddApplication(...)` para registrar seus handlers/serviços.
- A Infrastructure pode oferecer `AddInfrastructure(configuration)` para registrar `DbContext`, repositórios e clientes externos.
- A API chama esses métodos no `Program.cs`.

Exemplo ilustrativo:

```csharp
// Program.cs
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
```

Use os namespaces e assinaturas reais do projeto. Escolha lifetimes coerentes: serviços sem estado normalmente podem ser `Scoped` ou `Transient`, e `DbContext` normalmente é `Scoped` em aplicações web. Evite singletons que dependam de serviços scoped.

## 8. Banco de dados e EF Core

O provedor de banco não foi especificado neste guia. **Não presuma SQL Server**: determine o banco usado pelo repositório ou pergunte se a decisão for necessária.

Ao implementar persistência:

1. Configure o `DbContext` e o provedor na Infrastructure.
2. Mantenha connection strings fora do código-fonte; use configuração segura apropriada ao ambiente.
3. Passe `CancellationToken` às operações assíncronas compatíveis.
4. Crie migrações apenas se o projeto usar EF Core e a mudança exigir isso.
5. Evite executar migrações automaticamente em produção sem uma estratégia explícita de implantação.
6. Considere índices, constraints e concorrência conforme os requisitos reais.
7. Não adicione um repositório genérico ou Unit of Work apenas por convenção: use-os quando agregarem valor e respeite a estratégia de persistência existente.

## 9. Tratamento de erros e validação

- Defina uma estratégia consistente para erros de domínio, validação, autenticação, autorização e falhas inesperadas.
- Em APIs ASP.NET Core, considere `ProblemDetails` para respostas de erro padronizadas.
- Não exponha stack traces, segredos, SQL ou detalhes internos ao cliente em produção.
- Não converta toda exceção em `404` ou `400`; mapeie cada categoria para o resultado correto.
- Valide entrada HTTP e mantenha invariantes de negócio protegidas no Domain/Application.
- Use a solução de validação existente. Se nenhuma existir, prefira começar simples antes de adicionar uma dependência.

## 10. Testes

Crie ou atualize testes compatíveis com o projeto.

### Testes de domínio
- Testam invariantes, transições de estado e regras de entidades/value objects.
- Não precisam de banco de dados ou servidor HTTP.

### Testes de aplicação
- Testam casos de uso com dependências simuladas ou implementações em memória.
- Cobrem sucesso, item inexistente, entradas inválidas e falhas relevantes.

### Testes de integração
- Verificam endpoints, serialização, injeção de dependência e integração com infraestrutura.
- Use ambiente e banco de teste apropriados; não conecte testes a dados de produção.
- Não substitua testes de integração por mocks quando o objetivo for validar o comportamento real da integração.

Não escolha um framework de testes novo sem verificar os padrões existentes.

## 11. Requisitos de qualidade

Durante a implementação:

- Use nullable reference types e análise de compilação conforme o padrão do repositório.
- Mantenha nomes, namespaces, formatação e idioma de código consistentes com o projeto.
- Use `async`/`await` quando necessário e propague `CancellationToken` em operações assíncronas compatíveis.
- Evite métodos grandes, dependências circulares, service locator e estado global mutável.
- Não adicione pacotes NuGet sem necessidade concreta.
- Não altere contratos públicos sem verificar os impactos e testes.
- Não introduza CQRS, mensageria, cache, eventos de domínio ou microserviços sem requisitos que justifiquem a complexidade.

## 12. Fluxo de trabalho obrigatório do Codex

Siga esta sequência:

1. **Inspecionar:** listar a estrutura da solução, projetos, referências, configurações e testes.
2. **Diagnosticar:** identificar violações arquiteturais concretas e necessidades funcionais.
3. **Planejar:** informar quais arquivos/projetos serão criados ou modificados e por quê.
4. **Implementar:** realizar mudanças pequenas, seguindo os princípios deste documento e os padrões já existentes.
5. **Verificar:** executar build e testes; revisar referências entre camadas e dependências adicionadas.
6. **Resumir:** informar o que foi feito, decisões arquiteturais, comandos executados e resultados.
7. **Declarar pendências:** listar testes não executados, configurações que dependem do ambiente e decisões que precisam do usuário.

## 13. Critérios de aceite

Considere a implementação concluída quando:

- [ ] A solução compila com o SDK/framework esperado.
- [ ] As dependências entre projetos seguem a direção definida.
- [ ] Domain não depende de Application, Infrastructure ou API.
- [ ] Application não depende de Infrastructure ou API.
- [ ] A API não acessa diretamente o banco de dados nem repositórios concretos.
- [ ] Regras de negócio não estão concentradas nos endpoints/controllers.
- [ ] Dependências são registradas corretamente no composition root.
- [ ] Configurações sensíveis não estão hardcoded.
- [ ] Os casos de uso relevantes possuem testes.
- [ ] Testes e build foram executados, e seus resultados foram relatados com precisão.
- [ ] Não foram introduzidas bibliotecas ou abstrações desnecessárias.
- [ ] A documentação foi atualizada quando a implementação exigir instruções adicionais.

## 14. Relatório final esperado

Ao terminar, responda com:

1. **Resumo:** principais mudanças realizadas.
2. **Arquitetura:** projetos e responsabilidades implementados.
3. **Decisões:** escolhas relevantes e justificativas.
4. **Validação:** comandos de build/teste executados e respectivos resultados.
5. **Pendências:** configurações, migrações ou decisões que ainda dependem de ação humana.

---

## Instrução curta para iniciar a tarefa

> Leia este documento e inspecione primeiro o repositório atual. Implemente ou adapte a solução para seguir Clean Architecture em .NET 10, preservando os padrões e requisitos existentes. Não recrie projetos nem adicione bibliotecas sem necessidade. Siga a direção de dependências, mantenha regras de negócio fora da API, crie/atualize testes e execute build e testes ao final. Relate claramente as mudanças, os comandos executados e qualquer pendência.
