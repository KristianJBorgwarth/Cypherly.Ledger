using Ledger.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ledger.Infrastructure.Persistence.ModelConfigurations;

public sealed class LedgerEventModelConfiguration : BaseModelConfiguration<LedgerEvent>
{
    public override void Configure(EntityTypeBuilder<LedgerEvent> builder)
    {
        builder.ToTable("ledger_event");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever()
            .IsRequired();

        builder.Property(x => x.StreamId)
            .HasColumnName("stream_id")
            .IsRequired();

        builder.Property(x => x.Version)
            .HasColumnName("version")
            .IsRequired();

        builder.Property(x => x.PreviousHash)
            .HasColumnName("previous_hash")
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.Payload)
            .HasColumnName("payload")
            .IsRequired();

        builder.Property(x => x.WriteKeyPublic)
            .HasColumnName("write_key_public")
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.Signature)
            .HasColumnName("signature")
            .HasMaxLength(64)
            .IsRequired();

        builder.HasIndex(x => new { x.StreamId, x.Version })
            .IsUnique();

        base.Configure(builder);
    }
}
