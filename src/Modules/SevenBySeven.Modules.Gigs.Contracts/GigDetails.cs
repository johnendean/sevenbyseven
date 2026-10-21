namespace SevenBySeven.Modules.Gigs.Contracts;

/// <summary>
/// What I say about a Gig itself, apart from what was played at it. The limits it has to
/// meet are in <see cref="GigDetailsValidator"/>.
/// </summary>
public sealed record GigDetails
{
    public required DateOnly PlayedOn { get; init; }

    public string? Venue { get; init; }

    public string? Notes { get; init; }
}
