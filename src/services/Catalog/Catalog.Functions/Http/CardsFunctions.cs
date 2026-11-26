using System.Globalization;
using BuildingBlocks.Application;
using BuildingBlocks.Functions;
using Catalog.Application.UseCases.Cards;
using Catalog.Application.UseCases.Cards.GetCard;
using Catalog.Application.UseCases.Cards.ListCards;
using Catalog.Application.UseCases.Cards.RegisterCard;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Catalog.Functions.Http;

/// <summary>Rotas /api/catalog/cards. Fina: lê a entrada, valida, chama o handler e traduz o resultado.</summary>
public sealed class CardsFunctions(
    ICommandHandler<RegisterCardCommand, Guid> registerCard,
    IValidator<RegisterCardCommand> registerValidator,
    IQueryHandler<GetCardQuery, CardResponse> getCard,
    IQueryHandler<ListCardsQuery, PagedResult<CardResponse>> listCards,
    IValidator<ListCardsQuery> listValidator)
{
    [Function("RegisterCard")]
    public async Task<IActionResult> Register(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "catalog/cards")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        var (command, invalidBody) = await request.ReadJsonAsync<RegisterCardCommand>(cancellationToken);
        if (invalidBody is not null)
        {
            return invalidBody;
        }

        var validation = await registerValidator.ValidateAsync(command!, cancellationToken);
        if (!validation.IsValid)
        {
            return HttpResults.Validation(validation);
        }

        var result = await registerCard.Handle(command!, cancellationToken);
        return result.IsSuccess
            ? new CreatedResult($"/api/catalog/cards/{result.Value}", new { id = result.Value })
            : HttpResults.Problem(result.Error);
    }

    [Function("GetCard")]
    public async Task<IActionResult> Get(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "catalog/cards/{id:guid}")] HttpRequest request,
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await getCard.Handle(new GetCardQuery(id), cancellationToken);
        return result.IsSuccess ? new OkObjectResult(result.Value) : HttpResults.Problem(result.Error);
    }

    [Function("ListCards")]
    public async Task<IActionResult> List(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "catalog/cards")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        var query = new ListCardsQuery(
            request.Query["game"],
            request.Query["set"],
            request.Query["name"],
            ReadInt(request, "page", 1),
            ReadInt(request, "pageSize", 20));

        var validation = await listValidator.ValidateAsync(query, cancellationToken);
        if (!validation.IsValid)
        {
            return HttpResults.Validation(validation);
        }

        var result = await listCards.Handle(query, cancellationToken);
        return result.IsSuccess ? new OkObjectResult(result.Value) : HttpResults.Problem(result.Error);
    }

    // Ausente usa o padrão; valor não numérico vira 0 e o validador devolve 400 apontando o campo.
    private static int ReadInt(HttpRequest request, string name, int fallback)
    {
        var raw = request.Query[name].ToString();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return fallback;
        }

        return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : 0;
    }
}
