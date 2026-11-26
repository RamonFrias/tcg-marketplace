using BuildingBlocks.Application;
using BuildingBlocks.SharedKernel;
using Catalog.Application.Abstractions;
using Catalog.Domain.Cards;

namespace Catalog.Application.UseCases.Cards.GetCard;

public sealed record GetCardQuery(Guid Id) : IQuery<CardResponse>;

public sealed class GetCardHandler(ICardReadService reads) : IQueryHandler<GetCardQuery, CardResponse>
{
    public async Task<Result<CardResponse>> Handle(GetCardQuery query, CancellationToken cancellationToken)
    {
        var card = await reads.GetByIdAsync(query.Id, cancellationToken);
        return card is null ? CardErrors.NotFound : card;
    }
}
