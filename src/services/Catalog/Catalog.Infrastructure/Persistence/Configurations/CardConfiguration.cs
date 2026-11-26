using Catalog.Domain.Cards;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure.Persistence.Configurations;

internal sealed class CardConfiguration : IEntityTypeConfiguration<Card>
{
    public void Configure(EntityTypeBuilder<Card> builder)
    {
        builder.ToTable("cards");

        builder.HasKey(card => card.Id);
        builder.Property(card => card.Id)
            .HasConversion(id => id.Value, value => new CardId(value))
            .ValueGeneratedNever();

        builder.Property(card => card.Game).HasConversion<string>().HasMaxLength(20);
        builder.Property(card => card.SetCode).HasMaxLength(Card.MaxSetCodeLength);
        builder.Property(card => card.SetName).HasMaxLength(Card.MaxSetNameLength);
        builder.Property(card => card.CollectorNumber).HasMaxLength(Card.MaxCollectorNumberLength);
        builder.Property(card => card.Name).HasMaxLength(Card.MaxNameLength);
        builder.Property(card => card.Rarity).HasMaxLength(Card.MaxRarityLength);
        builder.Property(card => card.ImageUrl).HasMaxLength(Card.MaxImageUrlLength);

        // Um card canônico por (jogo, coleção, número): base do upsert da importação.
        builder.HasIndex(card => new { card.Game, card.SetCode, card.CollectorNumber }).IsUnique();
        builder.HasIndex(card => card.Name);

        builder.Ignore(card => card.DomainEvents);
    }
}
