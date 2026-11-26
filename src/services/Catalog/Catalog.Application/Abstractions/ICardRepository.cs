using Catalog.Domain.Cards;

namespace Catalog.Application.Abstractions;

/// <summary>Escrita de cards (agregado). A leitura para a API fica em <see cref="ICardReadService"/>.</summary>
public interface ICardRepository
{
    Task<bool> ExistsAsync(Game game, string setCode, string collectorNumber, CancellationToken cancellationToken);

    void Add(Card card);
}
