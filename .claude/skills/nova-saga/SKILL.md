---
name: nova-saga
description: Cria uma saga orquestrada com Azure Durable Functions no tcg-marketplace, com passos, compensações e timeouts (ex.: checkout, que reserva estoque, autoriza pagamento, confirma o pedido e compensa se algo falhar). Use quando um processo atravessa vários serviços e precisa de compensação, ou quando pedirem saga, orquestração, workflow ou Durable Functions.
argument-hint: <NomeDaSaga> [passos]
---

# Nova saga (Durable Functions, orquestração)

Pedido: `$ARGUMENTS`. A saga do checkout é da **Fase 2**; confira a fase em `.claude/project.json`.
Se a saga for de uma fase futura, avise o usuário antes de seguir.

## 1. Desenhar antes de codar

Mostre ao usuário uma tabela com uma linha por passo:

| # | Passo | Serviço | Como fala com ele | Sucesso | Falha | Compensação |
|---|---|---|---|---|---|---|

Defina também:
- o timeout de cada espera (a reserva de estoque expira em **20 min**);
- o estado final em cada caminho.

Exemplo do checkout:
1. `CreateOrder`, no Ordering.
2. `ReserveStock`, no Inventory: aguarda `StockReserved` ou `StockReservationFailed`.
3. `AuthorizePayment`, no Payment: aguarda `PaymentAuthorized` ou `PaymentFailed`.
4. `ConfirmOrder`.
5. `CreateShipments`, um por loja.

Compensações, de trás para frente: `ReleaseStock`, `RefundPayment`, `CancelOrder`.

## 2. Onde fica

- **Orquestrador:** no serviço dono do processo (checkout → `Ordering.Functions/Sagas/<Nome>/`).
- **Arquivos:**
  - `<Nome>Orchestrator.cs` com `[Function] RunOrchestrator([OrchestrationTrigger] TaskOrchestrationContext ctx)`;
  - `<Nome>Activities.cs` com as activities (`[ActivityTrigger]`), que só chamam handlers do Application;
  - `<Nome>Starter.cs`, que inicia a orquestração a partir de um evento ou de HTTP.
- **Id da instância:** determinístico a partir do negócio (`checkout-{orderId}`), para evitar saga duplicada.

## 3. Regras do orquestrador (determinismo)

O orquestrador é reexecutado (replay). Dentro dele:
- **Proibido:** `DateTime.Now`, `Guid.NewGuid()`, `Random`, acesso a banco, HTTP, Service Bus, `Task.Delay` e log sem `ctx.CreateReplaySafeLogger`.
- **Use:**
  - `ctx.CurrentUtcDateTime`;
  - `ctx.NewGuid()`;
  - `ctx.CallActivityAsync` para qualquer efeito;
  - `ctx.CreateTimer` para esperar;
  - `ctx.WaitForExternalEvent<T>("StockReserved")` para respostas de outros serviços.
- Espera com timeout: `Task.WhenAny(evento, timer)` e cancele o timer quando o evento chegar.
- **Retry** só nas activities, com `TaskOptions` e `RetryPolicy` (ex.: 3 tentativas, backoff exponencial). Erro de negócio não tem retry: vai para compensação.

## 4. Comunicação com outros serviços

- O orquestrador **não chama outro serviço direto**. Uma activity publica o comando ou evento pelo Outbox (skill `novo-evento`).
- A resposta chega num consumidor Service Bus do Ordering, que repassa para a instância:
  ```csharp
  client.RaiseEventAsync("checkout-{orderId}", "StockReserved", payload)
  ```
- Consumidores e activities são idempotentes, porque a mesma activity pode rodar mais de uma vez.

## 5. Compensação

- Guarde numa lista os passos já concluídos. Na falha, execute as compensações na ordem inversa.
- A compensação também precisa ser idempotente. Ela não pode falhar silenciosamente: se esgotar os retries, marque o pedido como `RequiresAttention` e registre o motivo.

## 6. Testes

- **Activities e handlers:** testes unitários normais.
- **Orquestrador:** testes com `TaskOrchestrationContext` mockado (NSubstitute). Cubra o caminho feliz, a falha em cada passo (com as compensações certas) e o timeout da reserva.

## 7. Registrar

- **ADR:** saga nova é decisão de arquitetura; use a skill `adr`.
- **Diagrama:** mostre o fluxo em mermaid ao usuário (`sequenceDiagram`).
