using System.Globalization;

namespace BuildingBlocks.SharedKernel;

/// <summary>
/// Valor monetário: quantia não negativa, arredondada em centavos, com moeda ISO 4217 (BRL, USD...).
/// Preço nunca é <see cref="decimal"/> solto no domínio.
/// </summary>
public sealed record Money
{
    public const string Brazil = "BRL";

    /// <summary>Teto de um valor criado a partir de entrada externa (evita overflow do decimal em somas e multiplicações).</summary>
    public const decimal MaxAmount = 1_000_000_000m;

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public decimal Amount { get; }

    public string Currency { get; }

    public static Result<Money> Create(decimal amount, string currency)
    {
        if (amount < 0)
        {
            return MoneyErrors.NegativeAmount;
        }

        if (amount > MaxAmount)
        {
            return MoneyErrors.TooLarge;
        }

        var normalized = (currency ?? string.Empty).Trim().ToUpperInvariant();
        if (normalized.Length != 3 || !normalized.All(char.IsAsciiLetterUpper))
        {
            return MoneyErrors.InvalidCurrency;
        }

        return new Money(Round(amount), normalized);
    }

    /// <summary>Atalho para valores em reais conhecidos pelo código (ex.: testes e seeds). Valor negativo é bug.</summary>
    public static Money Brl(decimal amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        return new Money(Round(amount), Brazil);
    }

    public static Money Zero(string currency = Brazil) => Create(0m, currency).Value;

    public Money Add(Money other)
    {
        ArgumentNullException.ThrowIfNull(other);
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Multiply(int quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(quantity);
        return new Money(Round(Amount * quantity), Currency);
    }

    public override string ToString() => $"{Currency} {Amount.ToString("0.00", CultureInfo.InvariantCulture)}";

    private static decimal Round(decimal amount) => Math.Round(amount, 2, MidpointRounding.AwayFromZero);

    private void EnsureSameCurrency(Money other)
    {
        if (other.Currency != Currency)
        {
            throw new InvalidOperationException($"Não dá para operar {Currency} com {other.Currency}.");
        }
    }
}

public static class MoneyErrors
{
    public static readonly Error NegativeAmount =
        Error.Validation("Money.NegativeAmount", "O valor não pode ser negativo.");

    public static readonly Error TooLarge =
        Error.Validation("Money.TooLarge", $"O valor não pode passar de {Money.MaxAmount:N0}.");

    public static readonly Error InvalidCurrency =
        Error.Validation("Money.InvalidCurrency", "A moeda precisa ser um código ISO 4217 de 3 letras, como BRL.");
}
