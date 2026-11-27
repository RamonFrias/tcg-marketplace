# Decisões do projeto

Respostas das 37 perguntas do brainstorm inicial, fechadas em 26/11/2026. Este arquivo é a **fonte da verdade**
das decisões de produto e de stack; o porquê de cada decisão de arquitetura fica nos ADRs desta pasta
([índice](README.md)).

Origem: página de decisões no claude.ai, copiada para o repositório para ficar versionada, revisável em PR e
legível por qualquer pessoa ou agente sem acesso ao claude.ai. Mudanças a partir daqui são feitas **neste arquivo**,
por PR, e a decisão de arquitetura nova ganha um ADR.

## A. Visão e escopo

| Tema | Decisão | ADR | Observação |
|---|---|---|---|
| Objetivo do projeto | Começar como estudo, já preparado para o Azure | [0001](0001-microsservicos-com-azure-functions.md) |  |
| Modelo de negócio | Marketplace com várias lojas e sub-pedido por loja | [0007](0007-servico-catalog.md) |  |
| Jogos no lançamento | Magic: The Gathering; Pokémon TCG; Yu-Gi-Oh!; One Piece; Lorcana |  |  |
| Atributos de uma oferta (listing) | Condição (NM/LP/MP/HP/DMG); Idioma; Foil / holo; Edição / coleção; Preço; Quantidade |  |  |
| Fonte dos dados dos cards | Seed fake + importação opcional |  |  |
| Front-end | Só API (Swagger / Postman) |  |  |

## B. Stack e ambiente local

| Tema | Decisão | ADR | Observação |
|---|---|---|---|
| Linguagem das Functions | C# (.NET isolated worker) | [0001](0001-microsservicos-com-azure-functions.md) |  |
| Como subir tudo localmente | .NET Aspire (AppHost + dashboard) | [0002](0002-aspire-para-o-ambiente-local.md) | Aspire **17**: AppHost com Microsoft.NET.Sdk e projetos por caminho (ADR 0002). |
| Banco de dados | Postgres em container | [0003](0003-postgres-com-um-banco-por-servico.md) |  |
| Isolamento do banco por serviço | Um database por serviço no mesmo container | [0003](0003-postgres-com-um-banco-por-servico.md) |  |
| Acesso a dados | EF Core | [0003](0003-postgres-com-um-banco-por-servico.md) | EF Core **10**: o 11 exige .NET 11 (ADR 0007). |
| Mensageria entre serviços | Azure Service Bus Emulator (topics + queues) | [0004](0004-mensageria-com-service-bus-e-outbox.md) |  |
| Motor de busca (Search) | OpenSearch |  |  |
| API Gateway local | YARP | [0001](0001-microsservicos-com-azure-functions.md) |  |
| Autenticação | JWT próprio (serviço Identity) | [0008](0008-fase-1-sem-autenticacao.md) | Login só a partir da Fase 2; a Fase 1 roda sem autenticação (ADR 0008). |
| Imagens dos cards | URL da API de origem |  |  |

## C. Serviços

| Tema | Decisão | ADR | Observação |
|---|---|---|---|
| Serviços do MVP | Catalog; Store; Inventory (Listings); Search; Cart; Ordering; Payment; Shipping; Notification; Identity | [0001](0001-microsservicos-com-azure-functions.md) |  |
| Extras para depois do MVP | Wishlist; Alerta de preço; Avaliação de lojas; Histórico de preço do card; Painel admin; Importar lista de deck para o carrinho |  |  |
| Papéis de usuário | Cliente; Lojista; Admin |  |  |

## D. Padrões e fluxo de compra

| Tema | Decisão | ADR | Observação |
|---|---|---|---|
| Saga do checkout | Orquestração com Durable Functions | [0004](0004-mensageria-com-service-bus-e-outbox.md) |  |
| Padrões que vamos adotar | Outbox; Consumidores idempotentes + Idempotency-Key; Concorrência otimista no estoque; Reserva de estoque com TTL; CQRS no Search; Dead-letter + reprocessamento; Correlation id + OpenTelemetry; Contratos de eventos versionados | [0004](0004-mensageria-com-service-bus-e-outbox.md) |  |
| Quando reservar o estoque | Só no checkout |  |  |
| Tempo de expiração da reserva | 20 minutos |  |  |
| Pagamento | Gateway simulado |  |  |
| Métodos de pagamento simulados | Cartão; Pix |  |  |
| Cálculo de frete | Tabela fixa por loja |  |  |
| Notificações locais | Só log / tabela |  |  |

## E. GitHub e processo

| Tema | Decisão | ADR | Observação |
|---|---|---|---|
| Organização do repositório | Monorepo | [0006](0006-fluxo-git-e-ci.md) |  |
| Nome do repositório | tcg-marketplace |  |  |
| Visibilidade | Privado |  | Mudou depois: o repositório ficou **público** em 26/11/2026. |
| Fluxo de branches | Trunk-based com PR curto | [0006](0006-fluxo-git-e-ci.md) |  |
| CI no GitHub Actions | Build + testes por serviço (path filters) | [0006](0006-fluxo-git-e-ci.md) |  |
| Tipos de teste | Unitário | [0005](0005-clean-architecture-ddd-e-tdd.md) |  |
| Registrar decisões como ADRs em docs/adr | Sim | [0000](0000-registro-de-decisoes.md) |  |

## F. Roadmap

| Tema | Decisão | ADR | Observação |
|---|---|---|---|
| Fases confirmadas | Fase 0 · Fundação; Fase 1 · Vitrine (Catalog, Store, Inventory, Search) |  | Primeiro vamos implementar a fase 0 e 1, depois vamos uma a uma nas restantes. Fase 0 mesclada; Fase 1 em andamento, um PR por serviço. |
| Ritmo / prazo esperado | 1 semana |  |  |

## G. Anotações livres

| Tema | Decisão | ADR | Observação |
|---|---|---|---|
| Ideias, dúvidas e pontos que não estão na lista | Temos que usar os padrões de TDD, DDD, Design System, Design Pattern, Clean Archtecture, SOLID, entre outros do mercado de trabalho |  |  |
