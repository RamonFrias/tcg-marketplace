# 0001. Microsserviços com Azure Functions em C#

- **Status:** Aceito
- **Data:** 2026-11-26
- **Serviços afetados:** todos

## Contexto

O tcg-marketplace é um marketplace de cards (Magic, Pokémon, Yu-Gi-Oh!, One Piece, Lorcana) com várias lojas. O objetivo é estudar microsserviços, mensageria e padrões de mercado, tudo rodando local, mas preparado para ir ao Azure.

## Decisão

- Cada contexto de negócio é um microsserviço: Catalog, Store, Inventory, Search, Cart, Ordering, Payment, Shipping, Notification e Identity.
- Cada serviço é um Function App em C# (.NET 10, isolated worker), dividido em `<Svc>.Domain`, `<Svc>.Application`, `<Svc>.Infrastructure` e `<Svc>.Functions` (ver ADR 0005).
- Um gateway YARP (`src/Gateway`) é a porta de entrada única: `/api/<servico>/...` vai para o Function App do serviço.
- O repositório é um monorepo; o que é comum fica em `src/building-blocks` (SharedKernel, Application, Contracts).
- O front-end fica fora do escopo por enquanto: a entrega é só API.

## Alternativas consideradas

| Alternativa | Por que não |
|---|---|
| Monólito modular | Mais simples, mas não exercita mensageria e consistência eventual, que são o foco do estudo |
| ASP.NET Core Web API por serviço | Funciona, mas o objetivo inclui aprender Azure Functions e Durable Functions |
| Um repositório por serviço | Na locadora, camadas copiadas entre repos obrigaram a mudar tudo duas vezes |
| Gateway com Azure API Management | Não roda local |

## Consequências

- **Mais fácil:** evoluir e testar cada serviço isolado; ir para o Azure sem reescrever.
- **Mais difícil:** consistência entre serviços só por eventos; mais processos para subir localmente (ver ADR 0002).
- **A fazer:** criar os serviços da Fase 1 com a skill `novo-servico`.
