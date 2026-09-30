using SevenBySeven.Modules.Catalogue.Domain;
using SevenBySeven.Modules.Collection.Domain;

namespace SevenBySeven.Modules.Gigs.Domain;

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
    Release Release,
    bool IsFormer,
    Repeat Repeat,
    bool PlayedEarlierThisGig);

/// <summary>A Copy that could be added to a Gig, and what adding it would amount to.</summary>
public sealed record Candidate(Copy Copy, Repeat Repeat, bool PlayedThisGig);
