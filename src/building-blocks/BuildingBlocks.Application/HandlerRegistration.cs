using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Application;

public static class HandlerRegistration
{
    private static readonly Type[] HandlerInterfaces =
    [
        typeof(ICommandHandler<>),
        typeof(ICommandHandler<,>),
        typeof(IQueryHandler<,>),
    ];

    /// <summary>
    /// Registra como scoped todo handler concreto de command e query do assembly
    /// (o Application de cada serviço chama isso no seu AddApplication).
    /// </summary>
    public static IServiceCollection AddHandlersFromAssembly(this IServiceCollection services, Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assembly);

        var handlers = assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false });

        foreach (var implementation in handlers)
        {
            var contracts = implementation.GetInterfaces()
                .Where(i => i.IsGenericType && HandlerInterfaces.Contains(i.GetGenericTypeDefinition()));

            foreach (var contract in contracts)
            {
                services.AddScoped(contract, implementation);
            }
        }

        return services;
    }
}
