using BuildingBlocks.Application;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Catalog.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddCatalogApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddHandlersFromAssembly(assembly);
        services.AddValidatorsFromAssembly(assembly, ServiceLifetime.Singleton);
        services.TryAddSingleton(TimeProvider.System);

        return services;
    }
}
