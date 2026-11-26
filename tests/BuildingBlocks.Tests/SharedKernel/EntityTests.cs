using BuildingBlocks.SharedKernel;

namespace BuildingBlocks.Tests.SharedKernel;

public class EntityTests
{
    private readonly record struct CardId(Guid Value);

    private sealed class Card(CardId id) : AggregateRoot<CardId>(id)
    {
        public void Publish() => RaiseDomainEvent(new CardPublished(Id, DateTimeOffset.UnixEpoch));
    }

    private sealed class Store(CardId id) : Entity<CardId>(id);

    private sealed record CardPublished(CardId CardId, DateTimeOffset OccurredAt) : IDomainEvent;

    private static readonly CardId SomeId = new(Guid.NewGuid());

    [Fact]
    public void Igualdade_MesmoTipoEMesmoId_SaoIguais()
    {
        var a = new Card(SomeId);
        var b = new Card(SomeId);

        Assert.Equal(a, b);
        Assert.True(a == b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Igualdade_IdsDiferentes_SaoDiferentes()
    {
        var a = new Card(SomeId);
        var b = new Card(new CardId(Guid.NewGuid()));

        Assert.NotEqual(a, b);
        Assert.True(a != b);
    }

    [Fact]
    public void Igualdade_TiposDiferentesComMesmoId_SaoDiferentes()
    {
        Entity<CardId> card = new Card(SomeId);
        Entity<CardId> store = new Store(SomeId);

        Assert.False(card.Equals(store));
        Assert.False(card == store);
    }

    [Fact]
    public void Construtor_IdPadrao_LancaArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new Card(default));
    }

    [Fact]
    public void Igualdade_ComNulo_EhFalsa()
    {
        var card = new Card(SomeId);

        Assert.False(card.Equals(null));
        Assert.False(card == null);
        Assert.True(card != null);
    }

    [Fact]
    public void RaiseDomainEvent_AdicionaOEventoNaLista()
    {
        var card = new Card(SomeId);

        card.Publish();

        var domainEvent = Assert.Single(card.DomainEvents);
        Assert.Equal(new CardPublished(SomeId, DateTimeOffset.UnixEpoch), domainEvent);
    }

    [Fact]
    public void ClearDomainEvents_EsvaziaALista()
    {
        var card = new Card(SomeId);
        card.Publish();

        card.ClearDomainEvents();

        Assert.Empty(card.DomainEvents);
    }

    [Fact]
    public void DomainEvents_NaoPodeSerAlteradoPorFora()
    {
        var card = new Card(SomeId);
        card.Publish();

        Assert.False(card.DomainEvents is ICollection<IDomainEvent> { IsReadOnly: false });
    }
}
