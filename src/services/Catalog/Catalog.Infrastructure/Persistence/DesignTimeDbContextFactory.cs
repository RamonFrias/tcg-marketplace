using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Catalog.Infrastructure.Persistence;

/// <summary>
/// Usado só pelo `dotnet ef` para gerar migrations. Gerar não conecta no banco; aplicar usa a connection string
/// da variável ConnectionStrings__catalogdb (a mesma que o Aspire injeta).
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<CatalogDbContext>
{
    public CatalogDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable($"ConnectionStrings__{DependencyInjection.ConnectionName}")
            ?? $"Host=localhost;Database={DependencyInjection.ConnectionName}";

        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        return new CatalogDbContext(options);
    }
}
