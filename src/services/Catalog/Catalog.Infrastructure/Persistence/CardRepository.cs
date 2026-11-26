using Catalog.Application.Abstractions;
using Catalog.Domain.Cards;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Persistence;

internal sealed class CardRepository(CatalogDbContext db) : ICardRepository
{
    public Task<bool> ExistsAsync(Game game, string setCode, string collectorNumber, CancellationToken cancellationToken) =>
        db.Cards.AnyAsync(
            card => card.Game == game && card.SetCode == setCode && card.CollectorNumber == collectorNumber,
            cancellationToken);

    public void Add(Card card) => db.Cards.Add(card);
}
