namespace SevenBySeven.Modules.Scanning.Domain;

/// <summary>
/// A single attempt to identify a Copy from a photograph. A Scan is ephemeral: it
/// lives in the user's circuit until it is confirmed or abandoned, and the photograph
/// is discarded either way — see docs/adr/0002.
/// </summary>
public sealed class Scan
{
    private const string JpegContentType = "image/jpeg";

    public Guid Id { get; } = Guid.CreateVersion7();

    public DateTimeOffset StartedAt { get; } = DateTimeOffset.UtcNow;

    public required ReadOnlyMemory<byte> Image { get; init; }

    public required string ContentType { get; init; }

    public required int Width { get; init; }

    public required int Height { get; init; }

    public required CaptureSource Source { get; init; }

    public int SizeBytes => Image.Length;

    /// <summary>
    /// Takes the JPEG the browser produced after downscaling. Kept here rather than in
    /// the page so it can be tested without a browser.
    /// </summary>
    /// <exception cref="FormatException">The bytes are empty or are not a JPEG.</exception>
    public static Scan FromJpeg(ReadOnlyMemory<byte> bytes, int width, int height, CaptureSource source)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

        if (bytes.IsEmpty)
        {
            throw new FormatException("The capture was empty.");
        }

        // Every JPEG opens with the start-of-image marker followed by a marker byte. The
        // pixels are the model's problem; this only rules out a truncated or wrong-format
        // transfer before it reaches identification.
        var span = bytes.Span;
        if (span.Length < 3 || span[0] != 0xFF || span[1] != 0xD8 || span[2] != 0xFF)
        {
            throw new FormatException("The capture is not a JPEG.");
        }

        return new Scan
        {
            Image = bytes,
            ContentType = JpegContentType,
            Width = width,
            Height = height,
            Source = source,
        };
    }

    /// <summary>Builds a data URL for display. The bytes are never written to disk.</summary>
    public string ToDataUrl() => $"data:{ContentType};base64,{Convert.ToBase64String(Image.Span)}";
}
