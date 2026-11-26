using BuildingBlocks.SharedKernel;

namespace BuildingBlocks.Tests.SharedKernel;

public class MoneyTests
{
    [Fact]
    public void Create_ValorNegativo_RetornaErroDeValidacao()
    {
        var result = Money.Create(-0.01m, "BRL");

        Assert.True(result.IsFailure);
        Assert.Equal(MoneyErrors.NegativeAmount, result.Error);
    }

    [Fact]
    public void Create_AcimaDoLimite_RetornaErroDeValidacao()
    {
        Assert.True(Money.Create(Money.MaxAmount, "BRL").IsSuccess);

        var result = Money.Create(Money.MaxAmount + 0.01m, "BRL");

        Assert.True(result.IsFailure);
        Assert.Equal(MoneyErrors.TooLarge, result.Error);
    }

    [Theory]
    [InlineData("")]
    [InlineData("R$")]
    [InlineData("BR")]
    [InlineData("REAL")]
    [InlineData("B1L")]
    public void Create_MoedaForaDoPadraoIso_RetornaErroDeValidacao(string currency)
    {
        var result = Money.Create(10m, currency);

        Assert.True(result.IsFailure);
        Assert.Equal(MoneyErrors.InvalidCurrency, result.Error);
    }

    [Fact]
    public void Create_MoedaEmMinusculasComEspacos_NormalizaParaMaiusculas()
    {
        var money = Money.Create(10m, " brl ").Value;

        Assert.Equal("BRL", money.Currency);
    }

    [Theory]
    [InlineData(10.005, 10.01)]
    [InlineData(10.004, 10.00)]
    [InlineData(0.125, 0.13)]
    public void Create_MaisDeDuasCasas_ArredondaParaCentavos(decimal amount, decimal expected)
    {
        var money = Money.Create(amount, "BRL").Value;

        Assert.Equal(expected, money.Amount);
    }

    [Fact]
    public void Brl_CriaValorEmReais()
    {
        var money = Money.Brl(25.90m);

        Assert.Equal(25.90m, money.Amount);
        Assert.Equal("BRL", money.Currency);
    }

    [Fact]
    public void Brl_ValorNegativo_LancaArgumentException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Money.Brl(-1m));
    }

    [Fact]
    public void Zero_CriaValorZeradoNaMoeda()
    {
        var zero = Money.Zero("USD");

        Assert.Equal(0m, zero.Amount);
        Assert.Equal("USD", zero.Currency);
    }

    [Fact]
    public void Add_MesmaMoeda_SomaOsValores()
    {
        var total = Money.Brl(10.50m).Add(Money.Brl(0.75m));

        Assert.Equal(Money.Brl(11.25m), total);
    }

    [Fact]
    public void Add_MoedasDiferentes_LancaInvalidOperationException()
    {
        var reais = Money.Brl(10m);
        var dolares = Money.Create(10m, "USD").Value;

        Assert.Throws<InvalidOperationException>(() => reais.Add(dolares));
    }

    [Fact]
    public void Multiply_PorQuantidade_MultiplicaOValor()
    {
        var total = Money.Brl(3.33m).Multiply(3);

        Assert.Equal(Money.Brl(9.99m), total);
    }

    [Fact]
    public void Multiply_QuantidadeNegativa_LancaArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Money.Brl(1m).Multiply(-1));
    }

    [Fact]
    public void Igualdade_MesmoValorEMesmaMoeda_SaoIguais()
    {
        Assert.Equal(Money.Brl(5m), Money.Create(5.00m, "brl").Value);
        Assert.NotEqual(Money.Brl(5m), Money.Create(5m, "USD").Value);
    }

    [Fact]
    public void ToString_MostraMoedaEValorComDuasCasas()
    {
        Assert.Equal("BRL 1234.50", Money.Brl(1234.5m).ToString());
    }
}
