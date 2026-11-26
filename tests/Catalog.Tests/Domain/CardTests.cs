using Catalog.Domain.Cards;

namespace Catalog.Tests.Domain;

public class CardTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 26, 12, 0, 0, TimeSpan.Zero);

    private static Card Valid() =>
        Card.Create(Game.Magic, " DEM1 ", "Demonstração", " 007 ", " Dragão de Cristal ", "Rare", "https://img.exemplo/007.png", Now).Value;

    [Fact]
    public void Create_DadosValidos_NormalizaOsCampos()
    {
        var card = Valid();

        Assert.Equal(Game.Magic, card.Game);
        Assert.Equal("dem1", card.SetCode);
        Assert.Equal("Demonstração", card.SetName);
        Assert.Equal("007", card.CollectorNumber);
        Assert.Equal("Dragão de Cristal", card.Name);
        Assert.Equal("Rare", card.Rarity);
        Assert.Equal("https://img.exemplo/007.png", card.ImageUrl);
    }

    [Fact]
    public void Create_DadosValidos_GeraIdNovoACadaCard()
    {
        Assert.NotEqual(Valid().Id, Valid().Id);
        Assert.NotEqual(Guid.Empty, Valid().Id.Value);
    }

    [Fact]
    public void Create_DadosValidos_RegistraEventoCardRegistered()
    {
        var card = Valid();

        var registered = Assert.IsType<CardRegistered>(Assert.Single(card.DomainEvents));
        Assert.Equal(card.Id, registered.CardId);
        Assert.Equal(Now, registered.OccurredAt);
    }

    [Fact]
    public void Create_SemImagem_Aceita()
    {
        var result = Card.Create(Game.Pokemon, "dem1", "Demonstração", "4/102", "Lagarto de Fogo", "Holo Rare", null, Now);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.ImageUrl);
    }

    [Theory]
    [InlineData("", "Demonstração", "007", "Dragão", "Rare", "Card.SetCodeRequired")]
    [InlineData("dem1", " ", "007", "Dragão", "Rare", "Card.SetNameRequired")]
    [InlineData("dem1", "Demonstração", "", "Dragão", "Rare", "Card.CollectorNumberRequired")]
    [InlineData("dem1", "Demonstração", "007", "  ", "Rare", "Card.NameRequired")]
    [InlineData("dem1", "Demonstração", "007", "Dragão", "", "Card.RarityRequired")]
    public void Create_CampoObrigatorioVazio_RetornaErroDeValidacao(
        string setCode, string setName, string number, string name, string rarity, string expectedCode)
    {
        var result = Card.Create(Game.Magic, setCode, setName, number, name, rarity, null, Now);

        Assert.True(result.IsFailure);
        Assert.Equal(expectedCode, result.Error.Code);
    }

    [Fact]
    public void Create_NomeAcimaDoLimite_RetornaErro()
    {
        var name = new string('a', Card.MaxNameLength + 1);

        var result = Card.Create(Game.Magic, "dem1", "Demonstração", "007", name, "Rare", null, Now);

        Assert.Equal("Card.NameTooLong", result.Error.Code);
    }

    [Theory]
    [InlineData("http://img.exemplo/007.png")]
    [InlineData("img.exemplo/007.png")]
    [InlineData("ftp://img.exemplo/007.png")]
    public void Create_ImagemSemHttps_RetornaErro(string imageUrl)
    {
        var result = Card.Create(Game.Magic, "dem1", "Demonstração", "007", "Dragão", "Rare", imageUrl, Now);

        Assert.Equal(CardErrors.InvalidImageUrl, result.Error);
    }
}
