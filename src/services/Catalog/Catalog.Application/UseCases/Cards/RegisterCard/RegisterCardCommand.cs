using BuildingBlocks.Application;

namespace Catalog.Application.UseCases.Cards.RegisterCard;

/// <summary>Cadastra um card canônico no catálogo. Devolve o id do card criado.</summary>
public sealed record RegisterCardCommand(
    string Game,
    string SetCode,
    string SetName,
    string CollectorNumber,
    string Name,
    string Rarity,
    string? ImageUrl) : ICommand<Guid>;
