namespace BuildingBlocks.Application;

/// <summary>Uma página de resultados de uma consulta paginada.</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public const int MaxPageSize = 100;

    /// <summary>Limite de página: evita overflow no offset ((página - 1) × tamanho) e varreduras sem sentido.</summary>
    public const int MaxPage = 10_000;

    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
