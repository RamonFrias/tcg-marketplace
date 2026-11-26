using BuildingBlocks.Application;
using Catalog.Application.Abstractions;
using Catalog.Application.UseCases.Cards;
using Catalog.Domain.Cards;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Persistence;

internal sealed class CardReadService(CatalogDbContext db) : ICardReadService
{
    public async Task<CardResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var cardId = new CardId(id);
        var card = await db.Cards.AsNoTracking().FirstOrDefaultAsync(c => c.Id == cardId, cancellationToken);
        return card is null ? null : ToResponse(card);
    }

    public async Task<PagedResult<CardResponse>> ListAsync(CardFilter filter, CancellationToken cancellationToken)
    {
        var query = db.Cards.AsNoTracking();

        if (filter.Game is { } game)
        {
            query = query.Where(c => c.Game == game);
        }

        if (filter.SetCode is { } setCode)
        {
            query = query.Where(c => c.SetCode == setCode);
        }

        if (filter.Name is { } name)
        {
            var pattern = $"%{EscapeLike(name)}%";
            query = query.Where(c => EF.Functions.ILike(c.Name, pattern));
        }

        var total = await query.CountAsync(cancellationToken);
        var cards = await query
            .OrderBy(c => c.Name)
            .ThenBy(c => c.SetCode)
            .ThenBy(c => c.CollectorNumber)
            .ThenBy(c => c.Game)
            .ThenBy(c => c.Id)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<CardResponse>([.. cards.Select(ToResponse)], filter.Page, filter.PageSize, total);
    }

    private static CardResponse ToResponse(Card card) =>
        new(card.Id.Value, card.Game.ToString(), card.SetCode, card.SetName, card.CollectorNumber, card.Name, card.Rarity, card.ImageUrl);

    // O texto do usuário não pode virar curinga do LIKE (% e _), e a barra é o escape padrão do Postgres.
    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
}
