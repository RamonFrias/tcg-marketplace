using BuildingBlocks.Application;
using BuildingBlocks.SharedKernel;
using Catalog.Application.Abstractions;
using Catalog.Domain.Cards;
using FluentValidation;

namespace Catalog.Application.UseCases.Cards.ListCards;

/// <summary>Lista cards com filtros opcionais por jogo, coleção e trecho do nome, em páginas.</summary>
public sealed record ListCardsQuery(string? Game, string? SetCode, string? Name, int Page = 1, int PageSize = 20)
    : IQuery<PagedResult<CardResponse>>;

public sealed class ListCardsValidator : AbstractValidator<ListCardsQuery>
{
    public ListCardsValidator()
    {
        RuleFor(q => q.Page).InclusiveBetween(1, PagedResult<CardResponse>.MaxPage);
        RuleFor(q => q.PageSize).InclusiveBetween(1, PagedResult<CardResponse>.MaxPageSize);
        RuleFor(q => q.Game)
            .Must(game => Games.TryParse(game, out _))
            .When(q => !string.IsNullOrWhiteSpace(q.Game))
            .WithMessage("Jogo desconhecido. Use Magic, Pokemon, YuGiOh, OnePiece ou Lorcana.");
        RuleFor(q => q.SetCode).MaximumLength(Card.MaxSetCodeLength);
        RuleFor(q => q.Name).MaximumLength(Card.MaxNameLength);
    }
}

public sealed class ListCardsHandler(ICardReadService reads) : IQueryHandler<ListCardsQuery, PagedResult<CardResponse>>
{
    public async Task<Result<PagedResult<CardResponse>>> Handle(ListCardsQuery query, CancellationToken cancellationToken)
    {
        Game? game = null;
        if (!string.IsNullOrWhiteSpace(query.Game))
        {
            if (!Games.TryParse(query.Game, out var parsed))
            {
                return CardErrors.InvalidGame;
            }

            game = parsed;
        }

        var filter = new CardFilter(
            game,
            Blank(query.SetCode)?.ToLowerInvariant(),
            Blank(query.Name),
            query.Page,
            query.PageSize);

        return await reads.ListAsync(filter, cancellationToken);
    }

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
