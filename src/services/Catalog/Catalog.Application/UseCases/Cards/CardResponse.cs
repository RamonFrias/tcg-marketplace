namespace Catalog.Application.UseCases.Cards;

public sealed record CardResponse(
    Guid Id,
    string Game,
    string SetCode,
    string SetName,
    string CollectorNumber,
    string Name,
    string Rarity,
    string? ImageUrl);
