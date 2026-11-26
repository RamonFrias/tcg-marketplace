using Catalog.Domain.Cards;
using Catalog.Infrastructure.Seed;

namespace Catalog.Tests.Infrastructure;

public class CatalogSeedTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 26, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Load_TodoCardDoSeedEhValidoNoDominio()
    {
        var failures = CatalogSeed.Load()
            .Select(s => (s, result: s.ToCard(Now)))
            .Where(x => x.result.IsFailure)
            .Select(x => $"{x.s.Game}/{x.s.SetCode}/{x.s.CollectorNumber}: {x.result.Error.Code}")
            .ToList();

        Assert.Empty(failures);
    }

    [Fact]
    public void Load_CobreOsCincoJogosComPeloMenosDuasColecoes()
    {
        var cards = CatalogSeed.Load().Select(s => s.ToCard(Now).Value).ToList();

        foreach (var game in Enum.GetValues<Game>())
        {
            var sets = cards.Where(c => c.Game == game).Select(c => c.SetCode).Distinct().Count();
            Assert.True(sets >= 2, $"{game} tem {sets} coleção(ões) no seed.");
        }
    }

    [Fact]
    public void Load_NaoRepeteJogoColecaoENumero()
    {
        var keys = CatalogSeed.Load().Select(s => s.ToCard(Now).Value).Select(c => (c.Game, c.SetCode, c.CollectorNumber)).ToList();

        Assert.Equal(keys.Count, keys.Distinct().Count());
    }
}
