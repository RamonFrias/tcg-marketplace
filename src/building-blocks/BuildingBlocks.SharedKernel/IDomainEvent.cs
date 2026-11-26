namespace BuildingBlocks.SharedKernel;

/// <summary>
/// Fato que aconteceu dentro de um serviço (nome no passado: ListingCreated, StockReserved).
/// Não sai do serviço; para avisar outros serviços existe o evento de integração (BuildingBlocks.Contracts).
/// </summary>
public interface IDomainEvent
{
    DateTimeOffset OccurredAt { get; }
}
