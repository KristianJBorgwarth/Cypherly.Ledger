using Ledger.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ledger.Infrastructure.Persistence.ModelConfigurations;

public sealed class LedgerStreamModelConfiguration : BaseModelConfiguration<LedgerStream>
{
    public override void Configure(EntityTypeBuilder<LedgerStream> builder)
    {
        builder.ToTable("ledger_stream");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever()
            .IsRequired();

        builder.Property(x => x.HeadVersion)
            .HasColumnName("head_version")
            .IsConcurrencyToken()
            .IsRequired();

        builder.Property(x => x.HeadHash)
            .HasColumnName("head_hash")
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.Archived)
            .HasColumnName("archived")
            .IsRequired();

        builder.HasMany(x => x.WriteKeys)
            .WithOne()
            .HasForeignKey(x => x.StreamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Events)
            .WithOne()
            .HasForeignKey(x => x.StreamId)
            .OnDelete(DeleteBehavior.Cascade);

        base.Configure(builder);
    }
}
