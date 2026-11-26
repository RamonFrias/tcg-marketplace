using System.Text.Json;
using BuildingBlocks.SharedKernel;
using Catalog.Domain.Cards;
using Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Seed;

/// <summary>
/// Cards de exemplo (fictícios) para desenvolvimento local. Entram só quando a tabela está vazia;
/// a importação real das APIs públicas vem com a skill importar-cards.
/// </summary>
public static class CatalogSeed
{
    private const string ResourceName = "Catalog.Infrastructure.Seed.cards.json";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static IReadOnlyList<SeedCard> Load()
    {
        using var stream = typeof(CatalogSeed).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Recurso {ResourceName} não encontrado no assembly.");
        return JsonSerializer.Deserialize<List<SeedCard>>(stream, JsonOptions) ?? [];
    }

    internal static async Task SeedAsync(CatalogDbContext db, TimeProvider clock, CancellationToken cancellationToken)
    {
        if (await db.Cards.AnyAsync(cancellationToken))
        {
            return;
        }

        var now = clock.GetUtcNow();
        foreach (var seed in Load())
        {
            var card = seed.ToCard(now);
            if (card.IsSuccess)
            {
                card.Value.ClearDomainEvents();
                db.Cards.Add(card.Value);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}

public sealed record SeedCard(
    string Game,
    string SetCode,
    string SetName,
    string CollectorNumber,
    string Name,
    string Rarity,
    string? ImageUrl)
{
    public Result<Card> ToCard(DateTimeOffset now) =>
        Games.TryParse(Game, out var game)
            ? Card.Create(game, SetCode, SetName, CollectorNumber, Name, Rarity, ImageUrl, now)
            : CardErrors.InvalidGame;
}
