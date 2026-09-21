using Ledger.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ledger.Infrastructure.Persistence.ModelConfigurations;

public sealed class LedgerWriteKeyModelConfiguration : BaseModelConfiguration<LedgerWriteKey>
{
    public override void Configure(EntityTypeBuilder<LedgerWriteKey> builder)
    {
        builder.ToTable("ledger_write_key");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever()
            .IsRequired();

        builder.Property(x => x.StreamId)
            .HasColumnName("stream_id")
            .IsRequired();

        builder.Property(x => x.PublicKey)
            .HasColumnName("public_key")
            .HasMaxLength(32)
            .IsRequired();

        builder.HasIndex(x => new { x.StreamId, x.PublicKey })
            .IsUnique();

        base.Configure(builder);
    }
}
