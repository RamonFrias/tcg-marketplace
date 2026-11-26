using Catalog.Domain.Cards;
using FluentValidation;

namespace Catalog.Application.UseCases.Cards.RegisterCard;

/// <summary>Valida o formato da entrada e aponta todos os campos com problema de uma vez (400 na borda HTTP).</summary>
public sealed class RegisterCardValidator : AbstractValidator<RegisterCardCommand>
{
    public RegisterCardValidator()
    {
        RuleFor(c => c.Game)
            .Must(game => Games.TryParse(game, out _))
            .WithMessage("Jogo desconhecido. Use Magic, Pokemon, YuGiOh, OnePiece ou Lorcana.");

        RuleFor(c => c.SetCode).NotEmpty().MaximumLength(Card.MaxSetCodeLength);
        RuleFor(c => c.SetName).NotEmpty().MaximumLength(Card.MaxSetNameLength);
        RuleFor(c => c.CollectorNumber).NotEmpty().MaximumLength(Card.MaxCollectorNumberLength);
        RuleFor(c => c.Name).NotEmpty().MaximumLength(Card.MaxNameLength);
        RuleFor(c => c.Rarity).NotEmpty().MaximumLength(Card.MaxRarityLength);

        RuleFor(c => c.ImageUrl)
            .Must(url => Card.IsValidImageUrl(url!.Trim()))
            .When(c => !string.IsNullOrWhiteSpace(c.ImageUrl))
            .WithMessage("A imagem precisa ser uma URL https.");
    }
}
