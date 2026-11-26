# 0004. Mensageria com Service Bus, Outbox e consumidores idempotentes

- **Status:** Aceito
- **Data:** 2026-11-26
- **Serviços afetados:** todos os que publicam ou consomem eventos

## Contexto

Os serviços precisam reagir ao que acontece nos outros (estoque reservado, pedido criado, pagamento autorizado) sem chamadas síncronas em cadeia, e sem perder nem duplicar efeitos quando algo falha.

## Decisão

- **Transporte:** Azure Service Bus (Emulator no ambiente local). Um tópico por tipo de evento (`stock-reserved`) e uma subscription por serviço consumidor (`ordering`).
- **Contratos:** eventos de integração em `src/building-blocks/BuildingBlocks.Contracts`, implementando `IIntegrationEvent` (`EventId`, `OccurredAt`). Nome no passado; mudança incompatível gera versão nova (`StockReservedV2`). O hook `contracts-guard` pede confirmação ao remover campos.
- **Publicação:** padrão Outbox. O evento é gravado na tabela `outbox_messages` na mesma transação do agregado, e uma Function de timer publica no Service Bus com `MessageId = EventId`.
- **Consumo:** idempotente. O consumidor registra o `MessageId` em `inbox_messages` na mesma transação do efeito; mensagem repetida é ignorada. Erro permanente vai para a dead-letter.
- **Processos longos** (checkout): saga orquestrada com Durable Functions, com compensações (skill `nova-saga`, Fase 2).
- **Observabilidade:** correlation id propagado e traces via OpenTelemetry.

## Alternativas consideradas

| Alternativa | Por que não |
|---|---|
| Storage Queues (Azurite) | Sem tópicos/subscriptions nem dead-letter com o mesmo nível de controle |
| Publicar direto no Service Bus após o `SaveChanges` | Se o processo cair entre as duas operações, o evento se perde |
| Coreografia pura no checkout | Difícil de acompanhar e compensar; a orquestração deixa o fluxo explícito |

## Consequências

- **Mais fácil:** serviços desacoplados no tempo; nenhum evento perdido; reprocessamento seguro.
- **Mais difícil:** consistência eventual; mais tabelas e uma Function de dispatcher por serviço.
- **A fazer:** a infraestrutura de Outbox/Inbox entra junto com o primeiro evento da Fase 1 (skill `novo-evento`).
