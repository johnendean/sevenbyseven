namespace SevenBySeven.Modules.Gigs.Domain;

/// <summary>
/// One run of records played back to back within a Gig, in running order. Usually
/// seven, but nothing insists on it: a short or long Selection is still what happened.
/// </summary>
public sealed class Selection
{
    private readonly List<Play> _plays = [];

    public Guid Id { get; private set; } = Guid.CreateVersion7();

    public Guid GigId { get; private set; }

    /// <summary>Where this Selection falls in the Gig, counting from zero.</summary>
    public int Sequence { get; internal set; }

    /// <summary>The Plays in running order, for the same reason as <see cref="Gig.Selections"/>.</summary>
    public IReadOnlyList<Play> Plays => [.. _plays.OrderBy(play => play.Sequence)];

    internal Play Add(Guid copyId)
    {
        var play = new Play { CopyId = copyId, Sequence = _plays.Count };
        _plays.Add(play);

        return play;
    }

    internal bool Remove(Guid playId)
    {
        var play = _plays.Find(candidate => candidate.Id == playId);

        if (play is null)
        {
            return false;
        }

        _plays.Remove(play);
        Renumber(Plays);

        return true;
    }

    internal bool Move(Guid playId, bool earlier)
    {
        var ordered = Plays.ToList();
        var from = ordered.FindIndex(candidate => candidate.Id == playId);
        var to = earlier ? from - 1 : from + 1;

        if (from < 0 || to < 0 || to >= ordered.Count)
        {
            return false;
        }

        (ordered[from], ordered[to]) = (ordered[to], ordered[from]);
        Renumber(ordered);

        return true;
    }

    private static void Renumber(IEnumerable<Play> ordered)
    {
        var position = 0;
        foreach (var play in ordered)
        {
            play.Sequence = position++;
        }
    }
}
