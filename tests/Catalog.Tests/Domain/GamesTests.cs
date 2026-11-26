using Catalog.Domain.Cards;

namespace Catalog.Tests.Domain;

public class GamesTests
{
    [Theory]
    [InlineData("Magic", Game.Magic)]
    [InlineData("pokemon", Game.Pokemon)]
    [InlineData(" YUGIOH ", Game.YuGiOh)]
    [InlineData("OnePiece", Game.OnePiece)]
    [InlineData("lorcana", Game.Lorcana)]
    public void TryParse_NomeConhecido_IgnoraCaixaEEspacos(string value, Game expected)
    {
        Assert.True(Games.TryParse(value, out var game));
        Assert.Equal(expected, game);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Digimon")]
    [InlineData("0")]
    [InlineData("3")]
    [InlineData("+1")]
    [InlineData("Magic,Pokemon")]
    [InlineData("Magic,Magic")]
    [InlineData("Magic, Lorcana")]
    public void TryParse_ValorDesconhecidoOuNumero_RetornaFalso(string? value)
    {
        Assert.False(Games.TryParse(value, out _));
    }
}
