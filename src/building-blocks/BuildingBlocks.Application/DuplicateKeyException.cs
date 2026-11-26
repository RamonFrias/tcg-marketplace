namespace BuildingBlocks.Application;

/// <summary>
/// A infraestrutura lança esta exceção quando o banco recusa um registro por chave única duplicada
/// (ex.: dois pedidos simultâneos cadastrando o mesmo card). O handler converte em erro de conflito.
/// </summary>
public sealed class DuplicateKeyException : Exception
{
    public DuplicateKeyException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
