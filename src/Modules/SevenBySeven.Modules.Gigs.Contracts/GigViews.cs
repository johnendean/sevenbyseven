using SevenBySeven.Modules.Catalogue.Contracts;
using SevenBySeven.Modules.Collection.Contracts;

namespace SevenBySeven.Modules.Gigs.Contracts;

/// <summary>A Gig as a line in a list of them.</summary>
public sealed record GigSummary(Guid Id, DateOnly PlayedOn, string? Venue, int Selections, int Plays);

/// <summary>A Gig laid out as it was played, for reading and for filling in.</summary>
public sealed record GigSheet(
    Guid Id,
    DateOnly PlayedOn,
    string? Venue,
    string? Notes,
    IReadOnlyList<SelectionSheet> Selections)
{
    public int Plays => Selections.Sum(selection => selection.Plays.Count);
}

public sealed record SelectionSheet(Guid Id, int Number, IReadOnlyList<PlayLine> Plays);

/// <summary>
/// One Play as it reads on a Gig. The Repeat is judged against the Gigs before this one,
/// so it says whether the record was a Repeat when it was played rather than now.
/// </summary>
public sealed record PlayLine(
    Guid Id,
    int Number,
    Guid CopyId,
    ReleaseLine Release,
    bool IsFormer,
    Repeat Repeat,
    bool PlayedEarlierThisGig);

/// <summary>A Copy that could be added to a Gig, and what adding it would amount to.</summary>
public sealed record Candidate(Guid CopyId, ReleaseLine Release, Repeat Repeat, bool PlayedThisGig);
