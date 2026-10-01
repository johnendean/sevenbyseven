using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SevenBySeven.Modules.Gigs.Domain;
using SevenBySeven.Shared.Persistence;

namespace SevenBySeven.Modules.Gigs.Persistence;

internal sealed class PlayConfiguration : IEntityTypeConfiguration<Play>
{
    public void Configure(EntityTypeBuilder<Play> builder)
    {
        builder.ToTable("plays", Schemas.Gigs);

        builder.HasKey(p => p.Id);

        // The id is set when the Play is made, not by the database. Said out loud, so that
        // one added to a Gig already loaded is inserted rather than taken for an existing row.
        builder.Property(p => p.Id).ValueGeneratedNever();

        // Not unique, for the same reason as a Selection's running order.
        builder.HasIndex(p => new { p.SelectionId, p.Sequence });
        builder.HasIndex(p => p.CopyId);

        // A real foreign key across the module boundary, and a restrictive one: a played
        // Copy is kept as a Former Copy rather than deleted (docs/adr/0006), and the
        // database refuses to lose one even if something forgets that.
        builder.HasOne(p => p.Copy)
            .WithMany()
            .HasForeignKey(p => p.CopyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
