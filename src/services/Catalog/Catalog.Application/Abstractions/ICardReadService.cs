using BuildingBlocks.Application;
using Catalog.Application.UseCases.Cards;
using Catalog.Domain.Cards;

namespace Catalog.Application.Abstractions;

/// <summary>Leitura direta para a API (CQRS leve): projeta do banco para o DTO sem carregar o agregado.</summary>
public interface ICardReadService
{
    Task<CardResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<CardResponse>> ListAsync(CardFilter filter, CancellationToken cancellationToken);
}

/// <summary>Filtro já normalizado: código da coleção em minúsculas, textos sem espaços nas pontas, vazios como null.</summary>
public sealed record CardFilter(Game? Game, string? SetCode, string? Name, int Page, int PageSize);
