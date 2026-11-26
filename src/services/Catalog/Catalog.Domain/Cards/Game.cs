namespace Catalog.Domain.Cards;

/// <summary>Jogos de cards vendidos no marketplace.</summary>
public enum Game
{
    Magic = 1,
    Pokemon = 2,
    YuGiOh = 3,
    OnePiece = 4,
    Lorcana = 5,
}

public static class Games
{
    private static readonly Dictionary<string, Game> ByName =
        Enum.GetValues<Game>().ToDictionary(g => g.ToString(), StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Converte o nome exato do jogo, sem diferenciar maiúsculas. Não usa Enum.TryParse, que aceitaria
    /// números e listas com vírgula ("Magic,Pokemon" viraria 1|2 = YuGiOh).
    /// </summary>
    public static bool TryParse(string? value, out Game game)
    {
        game = default;
        var text = value?.Trim();
        return !string.IsNullOrEmpty(text) && ByName.TryGetValue(text, out game);
    }
}
