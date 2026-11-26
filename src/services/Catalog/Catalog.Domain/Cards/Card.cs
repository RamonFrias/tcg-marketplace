using BuildingBlocks.SharedKernel;

namespace Catalog.Domain.Cards;

/// <summary>
/// Card canônico do catálogo: existe uma vez por (jogo, coleção, número), independente de quem vende.
/// Condição, idioma, foil, preço e estoque são atributos da oferta (Listing) no Inventory, não do card.
/// </summary>
public sealed class Card : AggregateRoot<CardId>
{
    public const int MaxSetCodeLength = 20;
    public const int MaxSetNameLength = 200;
    public const int MaxCollectorNumberLength = 20;
    public const int MaxNameLength = 200;
    public const int MaxRarityLength = 50;
    public const int MaxImageUrlLength = 500;

    private Card(
        CardId id,
        Game game,
        string setCode,
        string setName,
        string collectorNumber,
        string name,
        string rarity,
        string? imageUrl)
        : base(id)
    {
        Game = game;
        SetCode = setCode;
        SetName = setName;
        CollectorNumber = collectorNumber;
        Name = name;
        Rarity = rarity;
        ImageUrl = imageUrl;
    }

    public Game Game { get; private set; }

    /// <summary>Código da coleção em minúsculas (ex.: "dmu", "base1").</summary>
    public string SetCode { get; private set; }

    public string SetName { get; private set; }

    /// <summary>Número do card na coleção, como texto: "107", "4/102", "SV001".</summary>
    public string CollectorNumber { get; private set; }

    /// <summary>Nome em inglês ou no idioma de referência. O idioma de cada oferta fica no Listing.</summary>
    public string Name { get; private set; }

    public string Rarity { get; private set; }

    public string? ImageUrl { get; private set; }

    public static Result<Card> Create(
        Game game,
        string setCode,
        string setName,
        string collectorNumber,
        string name,
        string rarity,
        string? imageUrl,
        DateTimeOffset now)
    {
        if (!Enum.IsDefined(game))
        {
            return CardErrors.InvalidGame;
        }

        var normalizedSetCode = Clean(setCode).ToLowerInvariant();
        var normalizedSetName = Clean(setName);
        var normalizedNumber = Clean(collectorNumber);
        var normalizedName = Clean(name);
        var normalizedRarity = Clean(rarity);
        var normalizedImage = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim();

        var error =
            Required(normalizedSetCode, CardErrors.SetCodeRequired, nameof(SetCode), MaxSetCodeLength)
            ?? Required(normalizedSetName, CardErrors.SetNameRequired, nameof(SetName), MaxSetNameLength)
            ?? Required(normalizedNumber, CardErrors.CollectorNumberRequired, nameof(CollectorNumber), MaxCollectorNumberLength)
            ?? Required(normalizedName, CardErrors.NameRequired, nameof(Name), MaxNameLength)
            ?? Required(normalizedRarity, CardErrors.RarityRequired, nameof(Rarity), MaxRarityLength)
            ?? ValidateImage(normalizedImage);
        if (error is not null)
        {
            return error;
        }

        var card = new Card(
            CardId.New(),
            game,
            normalizedSetCode,
            normalizedSetName,
            normalizedNumber,
            normalizedName,
            normalizedRarity,
            normalizedImage);

        card.RaiseDomainEvent(new CardRegistered(card.Id, game, card.SetCode, card.CollectorNumber, card.Name, now));
        return card;
    }

    /// <summary>URL https absoluta, usada também pelo validador da borda.</summary>
    public static bool IsValidImageUrl(string url) =>
        url.Length <= MaxImageUrlLength
        && Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && uri.Scheme == Uri.UriSchemeHttps;

    private static string Clean(string? value) => value?.Trim() ?? string.Empty;

    private static Error? Required(string value, Error whenEmpty, string field, int max)
    {
        if (value.Length == 0)
        {
            return whenEmpty;
        }

        return value.Length > max ? CardErrors.TooLong(field, max) : null;
    }

    private static Error? ValidateImage(string? url) =>
        url is null || IsValidImageUrl(url) ? null : CardErrors.InvalidImageUrl;
}
