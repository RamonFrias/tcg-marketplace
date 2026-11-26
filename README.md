# tcg-marketplace

Marketplace de cards colecionáveis (Magic, Pokémon, Yu-Gi-Oh!, One Piece, Lorcana): o cliente busca um card, vê as lojas que vendem e as ofertas de cada uma (condição, idioma, foil, preço), monta o carrinho com várias lojas e fecha um pedido dividido por loja.

Projeto de estudo de **microsserviços com Azure Functions**, mensageria e padrões de mercado (DDD, Clean Architecture, TDD, SOLID), rodando local e preparado para o Azure.

## Stack

| Área | Escolha |
|---|---|
| Serviços | C# / .NET 10, Azure Functions (isolated worker) |
| Ambiente local | Aspire 17 (dashboard com logs, métricas e traces) |
| Dados | Postgres, um banco por serviço, EF Core |
| Mensageria | Azure Service Bus (Emulator), Outbox, consumidores idempotentes |
| Busca | OpenSearch |
| Entrada da API | Gateway YARP (`/api/<servico>/...`) |
| Processos longos | Durable Functions (saga do checkout) |
| Testes | xUnit v3 + NSubstitute (Microsoft.Testing.Platform) |
| CI | GitHub Actions |

As decisões e o porquê de cada uma estão em [docs/adr](docs/adr/README.md).

## Fase atual

**Fase 1 · Vitrine** (Catalog, Store, Inventory, Search), um PR por serviço. Pronto: **Catalog**.

| Serviço | Rotas (pelo gateway) |
|---|---|
| Catalog | `POST /api/catalog/cards` · `GET /api/catalog/cards/{id}` · `GET /api/catalog/cards?game=&set=&name=&page=&pageSize=` · `GET /api/catalog/health` |

Na Fase 1 as rotas são abertas, sem login ([ADR 0008](docs/adr/0008-fase-1-sem-autenticacao.md)).

## Como rodar

Pré-requisitos: .NET SDK 10, Azure Functions Core Tools v4 e **Docker** em execução.

```powershell
dotnet run --project src/AppHost/TcgMarketplace.AppHost
```

O terminal mostra o link do dashboard do Aspire. O gateway responde em `/health` (Development).

Só os testes, sem Docker:

```powershell
dotnet test tcg-marketplace.slnx
```

## Estrutura

```
src/
  building-blocks/   SharedKernel (Entity, AggregateRoot, Result, Money), Application (commands/queries), Contracts (eventos)
  ServiceDefaults/   OpenTelemetry, health checks, service discovery, resiliência
  AppHost/           Orquestração local (Aspire)
  Gateway/           YARP
  services/          Um microsserviço por pasta, em 4 camadas (a partir da Fase 1)
tests/               Testes unitários
docs/adr/            Decisões de arquitetura
.claude/             Hooks, skills e agents do Claude Code usados no projeto
```

## Como contribuir

Toda mudança entra por PR a partir de uma branch (`feat/...`, `fix/...`, `chore/...`); a `main` é protegida. Commits seguem Conventional Commits em português.
