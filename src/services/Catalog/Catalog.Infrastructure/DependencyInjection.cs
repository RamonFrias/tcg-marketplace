using BuildingBlocks.Application;
using Catalog.Application.Abstractions;
using Catalog.Infrastructure.Persistence;
using Catalog.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Catalog.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Nome do banco no AppHost (postgres.AddDatabase("catalogdb")) e da connection string injetada.</summary>
    public const string ConnectionName = "catalogdb";

    public static IHostApplicationBuilder AddCatalogInfrastructure(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Integração do Aspire: connection string injetada, health check, retries e telemetria do Npgsql.
        builder.AddNpgsqlDbContext<CatalogDbContext>(
            ConnectionName,
            configureDbContextOptions: options => options.UseSnakeCaseNamingConvention());

        builder.Services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<CatalogDbContext>());
        builder.Services.AddScoped<ICardRepository, CardRepository>();
        builder.Services.AddScoped<ICardReadService, CardReadService>();

        return builder;
    }

    /// <summary>Só em Development: aplica as migrations pendentes e popula o seed se o catálogo estiver vazio.</summary>
    public static async Task PrepareCatalogDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var clock = scope.ServiceProvider.GetService<TimeProvider>() ?? TimeProvider.System;

        await db.Database.MigrateAsync(cancellationToken);
        await CatalogSeed.SeedAsync(db, clock, cancellationToken);
    }
}
