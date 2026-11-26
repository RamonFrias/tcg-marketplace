using BuildingBlocks.SharedKernel;

namespace BuildingBlocks.Tests.SharedKernel;

public class ResultTests
{
    private static readonly Error SomeError = Error.Validation("Teste.Invalido", "Valor inválido.");

    [Fact]
    public void Success_SemValor_IndicaSucessoSemErro()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(Error.None, result.Error);
    }

    [Fact]
    public void Failure_ComErro_IndicaFalhaEExpoeOErro()
    {
        var result = Result.Failure(SomeError);

        Assert.True(result.IsFailure);
        Assert.Equal(SomeError, result.Error);
    }

    [Fact]
    public void Failure_ComErroNone_LancaArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Result.Failure(Error.None));
        Assert.Throws<ArgumentException>(() => Result.Failure<int>(Error.None));
    }

    [Fact]
    public void Failure_ComErroNulo_LancaArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Result.Failure(null!));
        Assert.Throws<ArgumentNullException>(() => Result.Failure<int>(null!));
    }

    [Fact]
    public void Value_EmSucesso_RetornaOValor()
    {
        var result = Result.Success(42);

        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Value_EmFalha_LancaInvalidOperationException()
    {
        var result = Result.Failure<int>(SomeError);

        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void ConversaoImplicita_DeValor_CriaSucesso()
    {
        Result<string> result = "carta";

        Assert.True(result.IsSuccess);
        Assert.Equal("carta", result.Value);
    }

    [Fact]
    public void ConversaoImplicita_DeErro_CriaFalha()
    {
        Result<string> result = SomeError;

        Assert.True(result.IsFailure);
        Assert.Equal(SomeError, result.Error);
    }

    [Theory]
    [InlineData(ErrorType.Validation)]
    [InlineData(ErrorType.NotFound)]
    [InlineData(ErrorType.Conflict)]
    [InlineData(ErrorType.Failure)]
    public void Error_Fabricas_DefinemOTipoCerto(ErrorType type)
    {
        var error = type switch
        {
            ErrorType.Validation => Error.Validation("c", "m"),
            ErrorType.NotFound => Error.NotFound("c", "m"),
            ErrorType.Conflict => Error.Conflict("c", "m"),
            _ => Error.Failure("c", "m"),
        };

        Assert.Equal(type, error.Type);
        Assert.Equal("c", error.Code);
        Assert.Equal("m", error.Message);
    }
}
