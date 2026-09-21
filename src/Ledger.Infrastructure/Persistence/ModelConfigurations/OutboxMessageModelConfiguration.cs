using Ledger.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ledger.Infrastructure.Persistence.ModelConfigurations;

public sealed class OutboxMessageModelConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_message");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(x => x.Type)
            .HasColumnName("type")
            .IsRequired();

        builder.Property(x => x.Content)
            .HasColumnName("content")
            .IsRequired();

        builder.Property(x => x.OccurredOn)
            .HasColumnName("occurred_on")
            .IsRequired();

        builder.Property(x => x.ProcessedOn)
            .HasColumnName("processed_on");

        builder.Property(x => x.Error)
            .HasColumnName("error");
    }
}