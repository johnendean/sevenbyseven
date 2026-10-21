using System.ComponentModel.DataAnnotations;

namespace SevenBySeven.Modules.Gigs.Contracts;

/// <summary>What I say about a Gig itself, apart from what was played at it.</summary>
public sealed record GigDetails
{
    public required DateOnly PlayedOn { get; init; }

    [StringLength(200, ErrorMessage = "A venue can be at most 200 characters.")]
    public string? Venue { get; init; }

    [StringLength(4000, ErrorMessage = "Notes can be at most 4,000 characters.")]
    public string? Notes { get; init; }
}
