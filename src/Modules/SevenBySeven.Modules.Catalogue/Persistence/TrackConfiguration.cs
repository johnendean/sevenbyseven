using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SevenBySeven.Modules.Catalogue.Domain;
using SevenBySeven.Shared.Persistence;

namespace SevenBySeven.Modules.Catalogue.Persistence;

internal sealed class TrackConfiguration : IEntityTypeConfiguration<Track>
{
    public void Configure(EntityTypeBuilder<Track> builder)
    {
        builder.ToTable("tracks", Schemas.Catalogue);

        builder.HasKey(t => t.Id);

        builder.HasIndex(t => new { t.ReleaseId, t.Position }).IsUnique();

        // Supports "which tracks sit between 118 and 124 BPM".
        builder.HasIndex(t => t.Bpm);

        builder.Property(t => t.Position).HasMaxLength(20);
        builder.Property(t => t.Title).HasMaxLength(500);
        builder.Property(t => t.Bpm).HasPrecision(6, 2);
        builder.Property(t => t.BpmSource).HasConversion<string>().HasMaxLength(40);
    }
}
