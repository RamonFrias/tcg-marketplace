using BuildingBlocks.Application;
using Catalog.Application.Abstractions;
using Catalog.Application.UseCases.Cards;
using Catalog.Application.UseCases.Cards.GetCard;
using Catalog.Application.UseCases.Cards.ListCards;
using Catalog.Domain.Cards;
using NSubstitute;

namespace Catalog.Tests.Application;

public class QueryHandlerTests
{
    private readonly ICardReadService _reads = Substitute.For<ICardReadService>();

    private static CardResponse Response(Guid id) =>
        new(id, "Magic", "dem1", "Demonstração", "007", "Dragão de Cristal", "Rare", null);

    [Fact]
    public async Task GetCard_Existente_RetornaOCard()
    {
        var id = Guid.NewGuid();
        _reads.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(Response(id));

        var result = await new GetCardHandler(_reads).Handle(new GetCardQuery(id), CancellationToken.None);

        Assert.Equal(id, result.Value.Id);
    }

    [Fact]
    public async Task GetCard_Inexistente_RetornaNotFound()
    {
        var result = await new GetCardHandler(_reads).Handle(new GetCardQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(CardErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task ListCards_RepassaOFiltroNormalizado()
    {
        var page = new PagedResult<CardResponse>([Response(Guid.NewGuid())], 2, 10, 11);
        _reads.ListAsync(Arg.Any<CardFilter>(), Arg.Any<CancellationToken>()).Returns(page);

        var result = await new ListCardsHandler(_reads)
            .Handle(new ListCardsQuery("pokemon", " DEM1 ", " lagarto ", 2, 10), CancellationToken.None);

        Assert.Same(page, result.Value);
        await _reads.Received(1).ListAsync(
            new CardFilter(Game.Pokemon, "dem1", "lagarto", 2, 10),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListCards_FiltrosVazios_ViramNulos()
    {
        _reads.ListAsync(Arg.Any<CardFilter>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<CardResponse>([], 1, 20, 0));

        await new ListCardsHandler(_reads).Handle(new ListCardsQuery(" ", "", null, 1, 20), CancellationToken.None);

        await _reads.Received(1).ListAsync(new CardFilter(null, null, null, 1, 20), Arg.Any<CancellationToken>());
    }
}
