using SevenBySeven.Modules.Collection.Domain;

namespace SevenBySeven.Modules.Gigs.Domain;

/// <summary>
/// One of my Copies being played within a Selection. Recorded against the physical Copy,
/// but judged — for Repeats and for how often something has been played — across every
/// Copy of the same Master (see <see cref="SameRecord"/>).
/// </summary>
public sealed class Play
{
    public Guid Id { get; private set; } = Guid.CreateVersion7();

    public Guid SelectionId { get; private set; }

    /// <summary>Where this Play falls in its Selection, counting from zero.</summary>
    public int Sequence { get; internal set; }

    public required Guid CopyId { get; init; }

    public Copy? Copy { get; private set; }
}
