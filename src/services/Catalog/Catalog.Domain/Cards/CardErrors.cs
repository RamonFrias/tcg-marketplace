using BuildingBlocks.SharedKernel;

namespace Catalog.Domain.Cards;

public static class CardErrors
{
    public static readonly Error InvalidGame =
        Error.Validation("Card.InvalidGame", "Jogo desconhecido. Use Magic, Pokemon, YuGiOh, OnePiece ou Lorcana.");

    public static readonly Error SetCodeRequired = Error.Validation("Card.SetCodeRequired", "Informe o código da coleção.");

    public static readonly Error SetNameRequired = Error.Validation("Card.SetNameRequired", "Informe o nome da coleção.");

    public static readonly Error CollectorNumberRequired =
        Error.Validation("Card.CollectorNumberRequired", "Informe o número do card na coleção.");

    public static readonly Error NameRequired = Error.Validation("Card.NameRequired", "Informe o nome do card.");

    public static readonly Error RarityRequired = Error.Validation("Card.RarityRequired", "Informe a raridade.");

    public static readonly Error InvalidImageUrl =
        Error.Validation("Card.InvalidImageUrl", "A imagem precisa ser uma URL https.");

    public static readonly Error AlreadyRegistered =
        Error.Conflict("Card.AlreadyRegistered", "Já existe um card com este jogo, coleção e número.");

    public static readonly Error NotFound = Error.NotFound("Card.NotFound", "Card não encontrado.");

    public static Error TooLong(string field, int max) =>
        Error.Validation($"Card.{field}TooLong", $"{field} aceita no máximo {max} caracteres.");
}
