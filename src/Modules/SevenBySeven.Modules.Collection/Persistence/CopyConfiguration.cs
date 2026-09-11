using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SevenBySeven.Modules.Collection.Domain;
using SevenBySeven.Shared.Persistence;

namespace SevenBySeven.Modules.Collection.Persistence;

internal sealed class CopyConfiguration : IEntityTypeConfiguration<Copy>
{
    public void Configure(EntityTypeBuilder<Copy> builder)
    {
        builder.ToTable("copies", Schemas.Collection);

        builder.HasKey(c => c.Id);

        builder.HasIndex(c => c.ReleaseId);

        // A real foreign key across the module boundary: a Copy without its pressing
        // is meaningless, and Postgres should say so.
        builder.HasOne(c => c.Release)
            .WithMany()
            .HasForeignKey(c => c.ReleaseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(c => c.MediaCondition).HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.SleeveCondition).HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.PricePaid).HasPrecision(10, 2);
        builder.Property(c => c.PricePaidCurrency).HasMaxLength(3).IsFixedLength();
        builder.Property(c => c.PurchasedFrom).HasMaxLength(200);
        builder.Property(c => c.Location).HasMaxLength(200);
        builder.Property(c => c.Notes).HasMaxLength(4000);
    }
}
