using BuildingBlocks.SharedKernel;

namespace BuildingBlocks.Application;

/// <summary>Pedido que muda estado e não devolve dado além do sucesso ou erro.</summary>
public interface ICommand;

/// <summary>Pedido que muda estado e devolve um dado (ex.: o id criado).</summary>
public interface ICommand<TResponse>;

/// <summary>Pedido que só lê.</summary>
public interface IQuery<TResponse>;

public interface ICommandHandler<in TCommand>
    where TCommand : ICommand
{
    Task<Result> Handle(TCommand command, CancellationToken cancellationToken);
}

public interface ICommandHandler<in TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    Task<Result<TResponse>> Handle(TCommand command, CancellationToken cancellationToken);
}

public interface IQueryHandler<in TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    Task<Result<TResponse>> Handle(TQuery query, CancellationToken cancellationToken);
}

/// <summary>Confirma numa transação só tudo o que o caso de uso alterou (agregados e Outbox).</summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
