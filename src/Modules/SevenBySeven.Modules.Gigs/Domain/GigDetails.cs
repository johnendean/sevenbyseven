namespace SevenBySeven.Modules.Gigs.Domain;

/// <summary>What I say about a Gig itself, apart from what was played at it.</summary>
public sealed record GigDetails
{
    public required DateOnly PlayedOn { get; init; }

    public string? Venue { get; init; }

    public string? Notes { get; init; }
}
