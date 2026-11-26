using System.Text;
using BuildingBlocks.Functions;
using BuildingBlocks.SharedKernel;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BuildingBlocks.Tests.Functions;

public class HttpResultsTests
{
    [Theory]
    [InlineData(ErrorType.Validation, 400)]
    [InlineData(ErrorType.NotFound, 404)]
    [InlineData(ErrorType.Conflict, 409)]
    [InlineData(ErrorType.Failure, 500)]
    public void Problem_TipoDeErro_ViraStatusHttpComOCodigoDoErro(ErrorType type, int status)
    {
        var error = new Error("Card.Teste", "Mensagem de teste.", type);

        var result = Assert.IsType<ObjectResult>(HttpResults.Problem(error));
        var problem = Assert.IsType<ProblemDetails>(result.Value);

        Assert.Equal(status, result.StatusCode);
        Assert.Equal(status, problem.Status);
        Assert.Equal("Card.Teste", problem.Title);
    }

    [Theory]
    [InlineData(ErrorType.Validation)]
    [InlineData(ErrorType.NotFound)]
    [InlineData(ErrorType.Conflict)]
    public void Problem_ErroDoCliente_MostraAMensagem(ErrorType type)
    {
        var result = Assert.IsType<ObjectResult>(HttpResults.Problem(new Error("Card.Teste", "Mensagem de teste.", type)));

        Assert.Equal("Mensagem de teste.", Assert.IsType<ProblemDetails>(result.Value).Detail);
    }

    [Fact]
    public void Problem_ErroDeFalha_NaoExpoeAMensagemInterna()
    {
        var result = Assert.IsType<ObjectResult>(HttpResults.Problem(Error.Failure("Db.Down", "Timeout em 10.0.0.5")));
        var problem = Assert.IsType<ProblemDetails>(result.Value);

        Assert.DoesNotContain("10.0.0.5", problem.Detail);
    }

    [Fact]
    public void Validation_AgrupaErrosPorCampo()
    {
        var validation = new ValidationResult(
        [
            new ValidationFailure("Name", "Nome é obrigatório."),
            new ValidationFailure("Name", "Nome muito longo."),
            new ValidationFailure("Game", "Jogo desconhecido."),
        ]);

        var result = Assert.IsType<BadRequestObjectResult>(HttpResults.Validation(validation));
        var problem = Assert.IsType<ValidationProblemDetails>(result.Value);

        Assert.Equal(2, problem.Errors["Name"].Length);
        Assert.Single(problem.Errors["Game"]);
    }

    [Fact]
    public async Task ReadJsonAsync_CorpoValido_Desserializa()
    {
        var request = Request("{\"name\":\"Dragão\",\"quantity\":2}");

        var (body, error) = await request.ReadJsonAsync<Sample>(TestContext.Current.CancellationToken);

        Assert.Null(error);
        Assert.Equal(new Sample("Dragão", 2), body);
    }

    [Theory]
    [InlineData("{ nao e json")]
    [InlineData("")]
    [InlineData("null")]
    public async Task ReadJsonAsync_CorpoInvalido_Retorna400(string json)
    {
        var (body, error) = await Request(json).ReadJsonAsync<Sample>(TestContext.Current.CancellationToken);

        Assert.Null(body);
        var result = Assert.IsType<ObjectResult>(error);
        Assert.Equal(400, result.StatusCode);
    }

    private sealed record Sample(string Name, int Quantity);

    private static HttpRequest Request(string json)
    {
        var context = new DefaultHttpContext();
        context.Request.ContentType = "application/json";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(json));
        return context.Request;
    }
}
