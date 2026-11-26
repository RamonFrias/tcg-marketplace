using BuildingBlocks.Application;
using BuildingBlocks.SharedKernel;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Tests.Application;

public class HandlerRegistrationTests
{
    public sealed record CreateThing(string Name) : ICommand<Guid>;

    public sealed record DeleteThing(Guid Id) : ICommand;

    public sealed record GetThing(Guid Id) : IQuery<string>;

    public sealed class CreateThingHandler : ICommandHandler<CreateThing, Guid>
    {
        public Task<Result<Guid>> Handle(CreateThing command, CancellationToken cancellationToken) =>
            Task.FromResult(Result.Success(Guid.NewGuid()));
    }

    public sealed class DeleteThingHandler : ICommandHandler<DeleteThing>
    {
        public Task<Result> Handle(DeleteThing command, CancellationToken cancellationToken) =>
            Task.FromResult(Result.Success());
    }

    public sealed class GetThingHandler : IQueryHandler<GetThing, string>
    {
        public Task<Result<string>> Handle(GetThing query, CancellationToken cancellationToken) =>
            Task.FromResult(Result.Success("coisa"));
    }

    public abstract class AbstractHandler : IQueryHandler<GetThing, string>
    {
        public abstract Task<Result<string>> Handle(GetThing query, CancellationToken cancellationToken);
    }

    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddHandlersFromAssembly(typeof(HandlerRegistrationTests).Assembly);
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    [Fact]
    public void AddHandlersFromAssembly_RegistraCommandHandlerComResposta()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<CreateThing, Guid>>();

        Assert.IsType<CreateThingHandler>(handler);
    }

    [Fact]
    public void AddHandlersFromAssembly_RegistraCommandHandlerSemResposta()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        Assert.IsType<DeleteThingHandler>(scope.ServiceProvider.GetRequiredService<ICommandHandler<DeleteThing>>());
    }

    [Fact]
    public void AddHandlersFromAssembly_RegistraQueryHandlerEIgnoraClasseAbstrata()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        var handlers = scope.ServiceProvider.GetServices<IQueryHandler<GetThing, string>>().ToList();

        var handler = Assert.Single(handlers);
        Assert.IsType<GetThingHandler>(handler);
    }

    [Fact]
    public void AddHandlersFromAssembly_RegistraComoScoped()
    {
        using var provider = BuildProvider();
        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var a = scope1.ServiceProvider.GetRequiredService<ICommandHandler<CreateThing, Guid>>();
        var b = scope1.ServiceProvider.GetRequiredService<ICommandHandler<CreateThing, Guid>>();
        var c = scope2.ServiceProvider.GetRequiredService<ICommandHandler<CreateThing, Guid>>();

        Assert.Same(a, b);
        Assert.NotSame(a, c);
    }
}
