using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SevenBySeven.Modules.Gigs.Domain;
using SevenBySeven.Shared.Persistence;

namespace SevenBySeven.Modules.Gigs.Persistence;

internal sealed class GigConfiguration : IEntityTypeConfiguration<Gig>
{
    public void Configure(EntityTypeBuilder<Gig> builder)
    {
        builder.ToTable("gigs", Schemas.Gigs);

        builder.HasKey(g => g.Id);

        builder.HasIndex(g => g.PlayedOn);

        builder.Property(g => g.Venue).HasMaxLength(200);
        builder.Property(g => g.Notes).HasMaxLength(4000);

        builder.HasMany(g => g.Selections)
            .WithOne()
            .HasForeignKey(s => s.GigId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(Gig.Selections))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
