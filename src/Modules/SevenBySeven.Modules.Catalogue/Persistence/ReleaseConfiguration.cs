using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SevenBySeven.Modules.Catalogue.Domain;
using SevenBySeven.Shared.Persistence;

namespace SevenBySeven.Modules.Catalogue.Persistence;

internal sealed class ReleaseConfiguration : IEntityTypeConfiguration<Release>
{
    public void Configure(EntityTypeBuilder<Release> builder)
    {
        builder.ToTable("releases", Schemas.Catalogue);

        builder.HasKey(r => r.Id);

        builder.HasIndex(r => r.DiscogsReleaseId).IsUnique();
        builder.HasIndex(r => r.CatalogueNumber);
        builder.HasIndex(r => r.Barcode);

        builder.Property(r => r.Title).HasMaxLength(500);
        builder.Property(r => r.ArtistName).HasMaxLength(500);
        builder.Property(r => r.LabelName).HasMaxLength(300);
        builder.Property(r => r.CatalogueNumber).HasMaxLength(100);
        builder.Property(r => r.Country).HasMaxLength(100);
        builder.Property(r => r.FormatDescription).HasMaxLength(300);
        builder.Property(r => r.Barcode).HasMaxLength(64);

        // Release dates are known at varying precision, so the parts are stored
        // separately rather than forced into a date.
        builder.OwnsOne(r => r.Released, released =>
        {
            released.Property(d => d.Year).HasColumnName("released_year");
            released.Property(d => d.Month).HasColumnName("released_month");
            released.Property(d => d.Day).HasColumnName("released_day");
        });

        builder.Navigation(r => r.Released).IsRequired(false);

        builder.HasMany(r => r.Tracks)
            .WithOne()
            .HasForeignKey(t => t.ReleaseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(Release.Tracks))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
