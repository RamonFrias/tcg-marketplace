using BuildingBlocks.Application;
using BuildingBlocks.SharedKernel;
using Catalog.Application.Abstractions;
using Catalog.Domain.Cards;

namespace Catalog.Application.UseCases.Cards.RegisterCard;

public sealed class RegisterCardHandler(ICardRepository cards, IUnitOfWork unitOfWork, TimeProvider clock)
    : ICommandHandler<RegisterCardCommand, Guid>
{
    public async Task<Result<Guid>> Handle(RegisterCardCommand command, CancellationToken cancellationToken)
    {
        if (!Games.TryParse(command.Game, out var game))
        {
            return CardErrors.InvalidGame;
        }

        var created = Card.Create(
            game,
            command.SetCode,
            command.SetName,
            command.CollectorNumber,
            command.Name,
            command.Rarity,
            command.ImageUrl,
            clock.GetUtcNow());
        if (created.IsFailure)
        {
            return created.Error;
        }

        var card = created.Value;
        if (await cards.ExistsAsync(card.Game, card.SetCode, card.CollectorNumber, cancellationToken))
        {
            return CardErrors.AlreadyRegistered;
        }

        cards.Add(card);
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DuplicateKeyException)
        {
            // Outro cadastro do mesmo card chegou entre a checagem e o insert.
            return CardErrors.AlreadyRegistered;
        }

        return card.Id.Value;
    }
}
