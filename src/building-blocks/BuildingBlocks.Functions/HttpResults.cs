using System.Text.Json;
using BuildingBlocks.SharedKernel;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BuildingBlocks.Functions;

/// <summary>Converte erros de domínio e de validação no formato de resposta HTTP padrão da API (ProblemDetails).</summary>
public static class HttpResults
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static IActionResult Problem(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        var status = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError,
        };

        // Falha inesperada não expõe detalhes internos para quem chama.
        var detail = status == StatusCodes.Status500InternalServerError
            ? "Erro inesperado. Tente de novo; se persistir, informe o código."
            : error.Message;

        return new ObjectResult(new ProblemDetails { Status = status, Title = error.Code, Detail = detail })
        {
            StatusCode = status,
        };
    }

    public static IActionResult Validation(ValidationResult validation)
    {
        ArgumentNullException.ThrowIfNull(validation);

        var errors = validation.Errors
            .GroupBy(failure => failure.PropertyName)
            .ToDictionary(group => group.Key, group => group.Select(failure => failure.ErrorMessage).ToArray());

        return new BadRequestObjectResult(new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Request.Invalid",
            Detail = "Um ou mais campos são inválidos.",
        });
    }

    /// <summary>Lê o corpo JSON. Corpo vazio, nulo ou malformado vira 400 (ProblemDetails) em vez de exceção.</summary>
    public static async Task<(T? Body, IActionResult? Error)> ReadJsonAsync<T>(
        this HttpRequest request,
        CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            var body = await JsonSerializer.DeserializeAsync<T>(request.Body, JsonOptions, cancellationToken);
            return body is null ? (null, InvalidJson()) : (body, null);
        }
        catch (JsonException)
        {
            return (null, InvalidJson());
        }
    }

    private static IActionResult InvalidJson() =>
        Problem(Error.Validation("Request.InvalidJson", "O corpo da requisição precisa ser um JSON válido."));
}
