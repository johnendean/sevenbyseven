namespace SevenBySeven.Modules.Gigs.Domain;

/// <summary>
/// One occasion on which I DJ, on a given date and perhaps at a named venue. Holds its
/// Selections in the order they were played. A Gig may be entered long after the fact,
/// so nothing about it assumes it is tonight's.
/// </summary>
public sealed class Gig
{
    private readonly List<Selection> _selections = [];

    public Guid Id { get; private set; } = Guid.CreateVersion7();

    public required DateOnly PlayedOn { get; set; }

    public string? Venue { get; set; }

    public string? Notes { get; set; }

    /// <summary>
    /// The Selections in running order. Ordered by <see cref="Selection.Sequence"/> rather
    /// than trusted to the list, which is only as ordered as whatever loaded it.
    /// </summary>
    public IReadOnlyList<Selection> Selections => [.. _selections.OrderBy(selection => selection.Sequence)];

    /// <summary>Starts a new Selection after the last one.</summary>
    public Selection AddSelection()
    {
        var selection = new Selection { Sequence = _selections.Count };
        _selections.Add(selection);

        return selection;
    }

    /// <summary>
    /// Removes a Selection and every Play in it, closing up the running order behind it.
    /// False when this Gig has no such Selection.
    /// </summary>
    public bool RemoveSelection(Guid selectionId)
    {
        var selection = _selections.Find(candidate => candidate.Id == selectionId);

        if (selection is null)
        {
            return false;
        }

        _selections.Remove(selection);

        var position = 0;
        foreach (var remaining in Selections)
        {
            remaining.Sequence = position++;
        }

        return true;
    }

    /// <summary>
    /// Adds a Copy to the end of a Selection. Null when this Gig has no such Selection.
    /// Nothing here checks the Copy is still mine: that is the caller's to know.
    /// </summary>
    public Play? AddPlay(Guid selectionId, Guid copyId) =>
        _selections.Find(candidate => candidate.Id == selectionId)?.Add(copyId);

    /// <summary>False when no Selection of this Gig holds such a Play.</summary>
    public bool RemovePlay(Guid playId) =>
        _selections.Any(selection => selection.Remove(playId));

    /// <summary>
    /// Moves a Play one place earlier or later within its Selection. False when there is
    /// no such Play, or it is already at that end.
    /// </summary>
    public bool MovePlay(Guid playId, bool earlier) =>
        _selections.Any(selection => selection.Move(playId, earlier));
}
