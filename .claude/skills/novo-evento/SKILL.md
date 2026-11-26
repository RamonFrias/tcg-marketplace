---
name: novo-evento
description: Cria um evento de integração entre serviços do tcg-marketplace, com o contrato versionado em BuildingBlocks.Contracts, a publicação pelo Outbox no serviço que emite e a Azure Function consumidora (Service Bus) idempotente no serviço que recebe. Use quando um serviço precisa avisar ou reagir a outro (ex.: estoque reservado, pedido criado, pagamento autorizado, preço alterado).
argument-hint: <ServicoQuePublica> <NomeDoEvento> -> <ServicoQueConsome> [o que o consumidor faz]
---

# Novo evento de integração (Outbox → Service Bus → consumidor idempotente)

Pedido: `$ARGUMENTS`.

## Convenções

- **Nome:** no passado, em inglês e PascalCase (`StockReserved`, `OrderCreated`, `PaymentAuthorized`).
- **Contrato:** `src/building-blocks/BuildingBlocks.Contracts/<ServicoQuePublica>/<Nome>.cs`.
- **Tópico:** `<nome-em-kebab>` (`stock-reserved`), um por tipo de evento.
- **Subscription:** o nome do serviço consumidor em minúsculas (`ordering`).
- **Evento de integração ≠ evento de domínio.** O de domínio fica dentro do serviço. O de integração é público e só carrega o que os outros precisam: ids e valores, **nunca a entidade inteira**.

## 1. Contrato

```csharp
namespace BuildingBlocks.Contracts.Inventory;

public sealed record StockReserved(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid OrderId,
    Guid ListingId,
    int Quantity,
    DateTimeOffset ExpiresAt) : IIntegrationEvent;
```

- Só tipos simples: `Guid`, `string`, `int`, `decimal`, `DateTimeOffset`, records de contrato.
- **Mudança incompatível** (remover ou renomear campo, mudar tipo) **não altera o contrato existente**. Crie `StockReservedV2`, publique os dois até todos os consumidores migrarem e registre um ADR. O hook `contracts-guard` pede confirmação nesse caso.
- Adicionar um campo opcional no fim é compatível.

## 2. Publicação (serviço que emite)

1. No Application, o handler que causa o fato chama `IIntegrationEventPublisher.Publish(evento)`. A implementação, no Infrastructure, **grava em `outbox_messages` na mesma transação** do agregado (`SaveChanges` único).
2. No `<Svc>.Functions`, a Function `OutboxDispatcher` (TimerTrigger a cada 5s) lê as mensagens pendentes:
   - em lotes de 50, com `FOR UPDATE SKIP LOCKED`;
   - publica no tópico com `MessageId = EventId`, `Subject = nome do evento` e `CorrelationId` repassado;
   - marca `processed_at`.

   Se essa Function já existir no serviço, só registre o tipo novo.
3. Falha ao publicar: incrementa `attempts` e guarda o `error`, e a próxima execução tenta de novo. Nada de perder evento.

## 3. Consumo (serviço que recebe)

Em `<Consumidor>.Functions/Messaging/<Nome>Consumer.cs`:
```csharp
[Function("<Nome>Consumer")]
public async Task Run(
    [ServiceBusTrigger("<nome-kebab>", "<consumidor>", Connection = "messaging")] ServiceBusReceivedMessage message,
    ServiceBusMessageActions actions, CancellationToken ct)
```

- **Idempotência:**
  - Antes de processar, tente inserir `message.MessageId` em `inbox_messages`, na mesma transação do efeito.
  - Se já existir, complete a mensagem e saia sem repetir o efeito.
- Desserialize para o contrato e chame um **handler do Application**: `Handle<Nome>`, com a regra no domínio.
- **Erro transitório** (banco fora): deixe a exceção subir. O Service Bus tenta de novo e, após o `MaxDeliveryCount`, manda para a **dead-letter**.
- **Erro permanente** (mensagem inválida): mande direto para a dead-letter com motivo (`actions.DeadLetterMessageAsync`).
- Log com `CorrelationId` e `EventId`.

## 4. Infra local

No AppHost, registre o tópico e a subscription no emulador:
```csharp
messaging.AddServiceBusTopic("<nome-kebab>").AddServiceBusSubscription("<consumidor>");
```
Confirme os nomes dos métodos na versão do Aspire usada no `Directory.Packages.props` com o agent `pesquisador-dotnet`.

## 5. Testes

- **Publicador:** o handler grava a mensagem no Outbox, usando um fake de `IIntegrationEventPublisher`.
- **Consumidor:**
  - o handler aplica o efeito;
  - a segunda entrega do mesmo `MessageId` não repete o efeito;
  - mensagem inválida vai para a dead-letter.

## 6. Registrar

Evento novo é contrato público: use a skill `adr` se ele muda o fluxo entre serviços.
Mostre ao usuário o fluxo: quem publica → tópico → quem consome → efeito.
