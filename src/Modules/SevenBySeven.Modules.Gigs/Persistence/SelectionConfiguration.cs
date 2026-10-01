using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SevenBySeven.Modules.Gigs.Domain;
using SevenBySeven.Shared.Persistence;

namespace SevenBySeven.Modules.Gigs.Persistence;

internal sealed class SelectionConfiguration : IEntityTypeConfiguration<Selection>
{
    public void Configure(EntityTypeBuilder<Selection> builder)
    {
        builder.ToTable("selections", Schemas.Gigs);

        builder.HasKey(s => s.Id);

        // The id is set when the Selection is made, not by the database. Said out loud, so that
        // one added to a Gig already loaded is inserted rather than taken for an existing row.
        builder.Property(s => s.Id).ValueGeneratedNever();

        // Not unique: closing up the running order renumbers rows one at a time, and
        // Postgres checks a unique index row by row rather than at the end.
        builder.HasIndex(s => new { s.GigId, s.Sequence });

        builder.HasMany(s => s.Plays)
            .WithOne()
            .HasForeignKey(p => p.SelectionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(Selection.Plays))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
