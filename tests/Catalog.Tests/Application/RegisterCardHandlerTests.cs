using BuildingBlocks.Application;
using Catalog.Application.Abstractions;
using Catalog.Application.UseCases.Cards.RegisterCard;
using Catalog.Domain.Cards;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Catalog.Tests.Application;

public class RegisterCardHandlerTests
{
    private readonly ICardRepository _cards = Substitute.For<ICardRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly TimeProvider _clock = Substitute.For<TimeProvider>();
    private readonly RegisterCardHandler _handler;

    public RegisterCardHandlerTests()
    {
        _clock.GetUtcNow().Returns(new DateTimeOffset(2026, 11, 26, 12, 0, 0, TimeSpan.Zero));
        _handler = new RegisterCardHandler(_cards, _unitOfWork, _clock);
    }

    private static RegisterCardCommand Command(string game = "Magic") =>
        new(game, "DEM1", "Demonstração", "007", "Dragão de Cristal", "Rare", null);

    [Fact]
    public async Task Handle_CardNovo_SalvaERetornaOId()
    {
        var result = await _handler.Handle(Command(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _cards.Received(1).Add(Arg.Is<Card>(c => c.Id.Value == result.Value && c.SetCode == "dem1"));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CardJaCadastrado_RetornaConflitoSemSalvar()
    {
        _cards.ExistsAsync(Game.Magic, "dem1", "007", Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(Command(), CancellationToken.None);

        Assert.Equal(CardErrors.AlreadyRegistered, result.Error);
        _cards.DidNotReceive().Add(Arg.Any<Card>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ChaveDuplicadaAoSalvar_RetornaConflito()
    {
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new DuplicateKeyException("cards", new InvalidOperationException()));

        var result = await _handler.Handle(Command(), CancellationToken.None);

        Assert.Equal(CardErrors.AlreadyRegistered, result.Error);
    }

    [Fact]
    public async Task Handle_JogoDesconhecido_RetornaErroSemSalvar()
    {
        var result = await _handler.Handle(Command("Digimon"), CancellationToken.None);

        Assert.Equal(CardErrors.InvalidGame, result.Error);
        _cards.DidNotReceive().Add(Arg.Any<Card>());
    }

    [Fact]
    public async Task Handle_RegraDoDominioViolada_DevolveOErroDoDominio()
    {
        var command = Command() with { Name = " " };

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal("Card.NameRequired", result.Error.Code);
        _cards.DidNotReceive().Add(Arg.Any<Card>());
    }
}
