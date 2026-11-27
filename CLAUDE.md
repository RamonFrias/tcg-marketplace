# tcg-marketplace

Marketplace de cards colecionáveis (Magic, Pokémon, Yu-Gi-Oh!, One Piece, Lorcana) com várias lojas: o **Catalog** guarda o card canônico (um por jogo + coleção + número) e cada loja vende **ofertas** dele (condição, idioma, foil, preço, estoque) no Inventory. Projeto de estudo de microsserviços em **C# / .NET 10 com Azure Functions**, rodando local com **Aspire 17**. As decisões do projeto estão em [docs/adr/decisoes.md](docs/adr/decisoes.md), o porquê de cada uma nos [ADRs](docs/adr/README.md), e a fase atual em `.claude/project.json`.

## Estrutura e camadas

```
src/services/<Svc>/       um microsserviço por pasta (modelo pronto: Catalog)
  <Svc>.Domain            agregados, value objects, regras, eventos de domínio. Só SharedKernel.
  <Svc>.Application       commands/queries + handlers, validadores, interfaces (repositório, leitura)
  <Svc>.Infrastructure    EF Core + Postgres (um banco por serviço), migrations, seed, mensageria
  <Svc>.Functions         borda HTTP e consumidores (Azure Functions isolated). Fina: valida, chama o handler, traduz o resultado
src/building-blocks/      SharedKernel (Entity, AggregateRoot, Result/Error, Money) · Application (ICommand, IQuery, PagedResult)
                          Contracts (eventos de integração entre serviços) · Functions (ProblemDetails, validação 400)
src/ServiceDefaults/      OpenTelemetry, health checks, service discovery, resiliência
src/AppHost/              topologia local (Aspire): containers, bancos, Function Apps, gateway
src/Gateway/              YARP: /api/<servico>/... → Function App do serviço
tests/<Svc>.Tests/        testes unitários (xUnit v3 + NSubstitute)
```

Dependências só para dentro: **Functions → Infrastructure → Application → Domain**.

## Regras

- **TDD:** teste antes do código; nome `Metodo_Cenario_Resultado` em português; só testes unitários.
- **Domínio:** fábrica estática que valida e devolve `Result<T>`; erro de regra é `Error`, nunca exceção; setters privados; ids tipados; dinheiro é `Money`.
- **Isolamento:** Domain e Application sem EF, Npgsql, Azure SDK ou ASP.NET; um serviço nunca referencia outro. Integração só por eventos em `BuildingBlocks.Contracts` (Outbox + consumidor idempotente).
- **Contratos:** evento de integração no passado; mudança incompatível vira versão nova (`StockReservedV2`).
- **Dados:** EF Core **10** (o 11 exige .NET 11), snake_case, schema = nome do serviço; migration revisada antes de aplicar.
- **Pacotes:** versões só em `Directory.Packages.props`; warnings são erros; estilo do `.editorconfig` cobrado no build.
- **Segredos:** nunca no repositório (user-secrets, variável de ambiente ou connection string do Aspire).
- **Git:** branch `<tipo>/<servico>-<descricao>`, Conventional Commits em português, **PR para tudo**. A `main` é protegida e exige CI verde; **o merge é sempre do dono**, nunca do Claude.
- **Fase 1 sem login** (ADR 0008): rotas abertas; o Identity entra na Fase 2.
- Decisão de arquitetura nova → ADR em `docs/adr`.

## Comandos

```powershell
dotnet build tcg-marketplace.slnx                      # build (warnings quebram)
dotnet test tcg-marketplace.slnx                       # todos os testes (Microsoft.Testing.Platform; sem --nologo)
dotnet test tests/Catalog.Tests                        # testes de um serviço
dotnet format whitespace . --folder --verify-no-changes --exclude .claude
node --test .claude/hooks/tests/hooks.test.js          # testes dos hooks
dotnet run --project src/AppHost/TcgMarketplace.AppHost   # sobe tudo (precisa de Docker em execução)
dotnet tool restore                                    # dotnet-ef local, para migrations
```

## Skills (`.claude/skills`)

| Skill | Quando usar |
|---|---|
| `novo-servico` | Criar o esqueleto de um microsserviço (4 camadas, testes, banco, AppHost, gateway) |
| `nova-entidade` | Modelar agregado, entidade ou value object com TDD |
| `novo-caso-de-uso` | Criar endpoint/operação: Function → command/query → repositório, com testes |
| `novo-evento` | Um serviço avisar ou reagir a outro (contrato, Outbox, consumidor idempotente) |
| `nova-migration` | Depois de mudar entidade ou mapeamento persistido |
| `importar-cards` | Seed do Catalog ou importação da Scryfall / pokemontcg.io |
| `nova-saga` | Processo entre serviços com compensação (checkout, Fase 2) |
| `adr` | Registrar uma decisão de arquitetura |
| `rodar-local` | Subir o ambiente, abrir o dashboard, diagnosticar container que não sobe |
| `entregar` | Só quando o usuário pedir: build, testes, branch, commit e PR |

## Agents (`.claude/agents`)

`arquiteto` revisa design (DDD, camadas, fronteiras) antes do PR · `qa` procura casos de borda e escreve testes (só em `tests/`) · `pesquisador-dotnet` confere na documentação oficial APIs que mudam muito (Aspire, Functions, EF) · `documentador` atualiza README e `docs/` (só `.md`, nunca ADR).

Hooks em `.claude/hooks` bloqueiam segredos, dependência proibida entre camadas, commit/push na `main` e comandos destrutivos; compilam e testam os serviços alterados ao fim de cada turno. Detalhes em [.claude/hooks/README.md](.claude/hooks/README.md).
