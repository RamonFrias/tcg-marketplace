using BuildingBlocks.SharedKernel;

namespace Catalog.Domain.Cards;

public sealed record CardRegistered(
    CardId CardId,
    Game Game,
    string SetCode,
    string CollectorNumber,
    string Name,
    DateTimeOffset OccurredAt) : IDomainEvent;
