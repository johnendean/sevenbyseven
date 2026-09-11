namespace SevenBySeven.Modules.Scanning.Domain;

/// <summary>
/// A single attempt to identify a Copy from a photograph. A Scan is ephemeral: it
/// lives in the user's circuit until it is confirmed or abandoned, and the photograph
/// is discarded either way.
/// </summary>
public sealed class Scan
{
    public Guid Id { get; } = Guid.CreateVersion7();

    public DateTimeOffset StartedAt { get; } = DateTimeOffset.UtcNow;

    public required ReadOnlyMemory<byte> Image { get; init; }

    public required string ContentType { get; init; }
}
