namespace BuildingBlocks.Contracts;

/// <summary>
/// Evento público entre serviços, publicado pelo Outbox no Service Bus.
/// <see cref="EventId"/> vira o MessageId da mensagem e é a chave de idempotência do consumidor.
/// Mudança incompatível gera uma versão nova (ex.: StockReservedV2); o contrato antigo não muda.
/// </summary>
public interface IIntegrationEvent
{
    Guid EventId { get; }

    DateTimeOffset OccurredAt { get; }
}
