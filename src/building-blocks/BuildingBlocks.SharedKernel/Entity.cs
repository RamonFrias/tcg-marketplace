namespace BuildingBlocks.SharedKernel;

/// <summary>
/// Objeto com identidade: duas entidades são iguais quando têm o mesmo tipo e o mesmo id.
/// O id é atribuído na criação (ids tipados sobre Guid), nunca gerado depois pelo banco;
/// por isso id com valor padrão é recusado.
/// </summary>
public abstract class Entity<TId> : IEquatable<Entity<TId>>
    where TId : notnull
{
    protected Entity(TId id)
    {
        if (EqualityComparer<TId>.Default.Equals(id, default!))
        {
            throw new ArgumentException("A entidade precisa de um id atribuído na criação.", nameof(id));
        }

        Id = id;
    }

    public TId Id { get; private init; }

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) => Equals(left, right);

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !Equals(left, right);

    public bool Equals(Entity<TId>? other) =>
        other is not null
        && other.GetType() == GetType()
        && EqualityComparer<TId>.Default.Equals(Id, other.Id);

    public override bool Equals(object? obj) => obj is Entity<TId> other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);
}
