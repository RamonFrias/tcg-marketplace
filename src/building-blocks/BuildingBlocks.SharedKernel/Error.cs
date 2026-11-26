namespace BuildingBlocks.SharedKernel;

/// <summary>Categoria do erro. A borda HTTP usa o tipo para escolher o status (400, 404, 409, 500).</summary>
public enum ErrorType
{
    Failure = 0,
    Validation = 1,
    NotFound = 2,
    Conflict = 3,
}

/// <summary>Erro de negócio ou de validação, com código estável (ex.: "Listing.InsufficientStock").</summary>
public sealed record Error(string Code, string Message, ErrorType Type)
{
    /// <summary>Ausência de erro. Usado só por resultados de sucesso.</summary>
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);

    public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);

    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);

    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);

    public static Error Failure(string code, string message) => new(code, message, ErrorType.Failure);
}
