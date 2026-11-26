using Catalog.Application.UseCases.Cards.ListCards;
using Catalog.Application.UseCases.Cards.RegisterCard;

namespace Catalog.Tests.Application;

public class ValidatorTests
{
    private static readonly RegisterCardCommand ValidCommand =
        new("Pokemon", "dem1", "Demonstração", "4/102", "Lagarto de Fogo", "Holo Rare", "https://img.exemplo/4.png");

    [Fact]
    public void RegisterCard_ComandoValido_Passa()
    {
        Assert.True(new RegisterCardValidator().Validate(ValidCommand).IsValid);
    }

    [Fact]
    public void RegisterCard_VariosProblemas_ApontaCadaCampo()
    {
        var command = ValidCommand with { Game = "Digimon", Name = "", ImageUrl = "http://inseguro" };

        var result = new RegisterCardValidator().Validate(command);

        var fields = result.Errors.Select(e => e.PropertyName).ToHashSet();
        Assert.Contains(nameof(RegisterCardCommand.Game), fields);
        Assert.Contains(nameof(RegisterCardCommand.Name), fields);
        Assert.Contains(nameof(RegisterCardCommand.ImageUrl), fields);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(3, 100)]
    public void ListCards_PaginaValida_Passa(int page, int pageSize)
    {
        Assert.True(new ListCardsValidator().Validate(new ListCardsQuery(null, null, null, page, pageSize)).IsValid);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    [InlineData(10_001, 20)]
    [InlineData(int.MaxValue, 100)]
    public void ListCards_PaginaForaDoLimite_Falha(int page, int pageSize)
    {
        Assert.False(new ListCardsValidator().Validate(new ListCardsQuery(null, null, null, page, pageSize)).IsValid);
    }

    [Fact]
    public void ListCards_JogoDesconhecido_Falha()
    {
        Assert.False(new ListCardsValidator().Validate(new ListCardsQuery("Digimon", null, null, 1, 20)).IsValid);
    }
}
