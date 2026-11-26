---
name: novo-servico
description: Cria um microsserviço novo do tcg-marketplace (Catalog, Store, Inventory, Search, Cart, Ordering, Payment, Shipping, Notification, Identity) com as 4 camadas da Clean Architecture, projeto de testes, banco próprio no Postgres, registro no AppHost do Aspire e rota no gateway YARP. Use quando pedirem para criar, iniciar ou montar o esqueleto de um serviço.
argument-hint: <NomeDoServico>
---

# Novo serviço

Serviço pedido: `$ARGUMENTS` (PascalCase, singular ou como está em `.claude/project.json` → `servicos`).
Se não vier nome, pergunte. Se o serviço já existir em `src/services/`, pare e avise.

## Pré-requisitos (Fase 0)

Confira se já existem. O que faltar, crie antes, numa etapa separada e avisando o usuário:

- `tcg-marketplace.slnx` na raiz.
- `Directory.Build.props`: `net10.0`, `Nullable=enable`, `ImplicitUsings=enable`, `TreatWarningsAsErrors=true`.
- `Directory.Packages.props`: Central Package Management. Versões **só** aqui; nos `.csproj` vai `<PackageReference Include="X" />` sem versão.
- `src/building-blocks/BuildingBlocks.SharedKernel`: `Entity<TId>`, `AggregateRoot<TId>`, `IDomainEvent`, `Result`/`Error`, `Money`. Value objects são `sealed record`.
- `src/building-blocks/BuildingBlocks.Application`: `ICommand`, `IQuery`, handlers, `IUnitOfWork` e `AddHandlersFromAssembly`.
- `src/building-blocks/BuildingBlocks.Contracts`: `IIntegrationEvent`.
- `src/ServiceDefaults/TcgMarketplace.ServiceDefaults`, `src/AppHost/TcgMarketplace.AppHost` (Aspire 17) e `src/Gateway/TcgMarketplace.Gateway` (YARP).
- Testes rodam pelo Microsoft.Testing.Platform (`global.json`): `dotnet test <projeto>`, sem `--nologo`. Projetos de teste usam só `xunit.v3` e `NSubstitute`, sem `Microsoft.NET.Test.Sdk`.

## Estrutura a gerar

```
src/services/<Svc>/
  <Svc>.Domain/            → refs: BuildingBlocks.SharedKernel                    (sem pacotes de infra)
  <Svc>.Application/       → refs: <Svc>.Domain, BuildingBlocks.Contracts           (sem pacotes de infra)
  <Svc>.Infrastructure/    → refs: <Svc>.Application  + EF Core/Npgsql/Service Bus
  <Svc>.Functions/         → refs: <Svc>.Infrastructure (Azure Functions isolated, v4)
tests/<Svc>.Tests/         → refs: <Svc>.Domain, <Svc>.Application (xUnit v3 + NSubstitute)
```

Regras que os hooks também cobram (`layer-guard`):
- O namespace de cada arquivo é o nome do projeto mais a pasta (`Inventory.Domain.Listings`).
- Nenhum projeto de um serviço referencia outro serviço. A integração passa por `BuildingBlocks.Contracts` e eventos.
- Domain e Application não usam EF Core, Npgsql, Azure SDK, OpenSearch nem ASP.NET.

## Passo a passo

1. **Criar os projetos** com `dotnet new`:
   - Domain, Application e Infrastructure: `classlib`.
   - Functions: `func init` com worker isolated, ou `dotnet new func`.
   - Testes: template xUnit.
   - Adicione todos à `tcg-marketplace.slnx` com `dotnet sln add`.
2. **Ajustar referências e pacotes** conforme a estrutura acima, sem versão no `.csproj`.
3. **Domain:** pasta vazia por agregado. Não invente entidades: elas nascem com a skill `nova-entidade`.
4. **Application:**
   - `DependencyInjection.cs` com `AddApplication(this IServiceCollection)`;
   - pastas `Abstractions/` (interfaces de repositório, `IUnitOfWork`) e `UseCases/`.
5. **Infrastructure:**
   - `Persistence/<Svc>DbContext.cs`, com schema padrão igual ao nome do serviço em minúsculas;
   - `Persistence/DesignTimeDbContextFactory.cs`, que lê a connection string da variável de ambiente `ConnectionStrings__<svc>db`;
   - `DependencyInjection.cs` com `AddInfrastructure(...)`;
   - tabelas `outbox_messages` e `inbox_messages`, preparadas para a skill `novo-evento`.
6. **Functions:**
   - `Program.cs` com `FunctionsApplication.CreateBuilder(args)`, `ConfigureFunctionsWebApplication()`, `AddServiceDefaults()` (se o ServiceDefaults existir), `AddApplication()` e `AddInfrastructure()`;
   - uma Function `Health` em `GET api/<svc>/health`.
7. **Banco e AppHost.** Em `src/AppHost/TcgMarketplace.AppHost/AppHost.cs` (Aspire 17):
   ```csharp
   var <svc>Db = postgres.AddDatabase("<svc>db");
   var <svc> = builder.AddAzureFunctionsProject("<svc>-functions", "../../services/<Svc>/<Svc>.Functions/<Svc>.Functions.csproj")
       .WithReference(<svc>Db).WaitFor(<svc>Db)
       .WithReference(messaging)          // Service Bus emulator, se o serviço publica/consome eventos
       .WithHostStorage(storage);
   gateway.WithReference(<svc>);
   ```
   - O caminho é relativo à pasta do AppHost. O AppHost **não** tem `ProjectReference` para os serviços.
   - Adicione `Aspire.Hosting.Azure.Functions` ao `.csproj` do AppHost (a versão já está no `Directory.Packages.props`).
   - A API do Aspire muda entre versões. Antes de escrever, peça ao agent `pesquisador-dotnet` os nomes certos dos métodos (`AddAzureFunctionsProject`, `WithHostStorage`) para a versão usada no `Directory.Packages.props`.
8. **Gateway:** rota YARP `/api/<svc>/{**catch-all}` → cluster `<svc>-functions`, com service discovery do Aspire.
9. **Testes:** um teste de fumaça em `tests/<Svc>.Tests`. Por exemplo, o `AddApplication` resolve sem erro.
10. **Verificar:** `dotnet build tcg-marketplace.slnx` e `dotnet test tests/<Svc>.Tests` passando.
11. **ADR:** serviço novo é decisão de arquitetura. Use a skill `adr` para registrar o motivo e as responsabilidades do serviço.

## Ao terminar

Resuma ao usuário:
- a árvore criada;
- o que foi registrado no AppHost e no gateway;
- o comando para subir (skill `rodar-local`);
- o que ficou pendente, como entidades e casos de uso.

Não commite. Commit é com a skill `entregar`, quando o usuário pedir.
