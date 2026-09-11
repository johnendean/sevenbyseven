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
    /// Parses the data URL the browser hands back after downscaling. Kept here rather
    /// than in the page so it can be tested without a browser.
    /// </summary>
    /// <exception cref="FormatException">The data URL is malformed or not base64 JPEG.</exception>
    public static Scan FromDataUrl(string dataUrl, int width, int height, CaptureSource source)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataUrl);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

        const string expectedPrefix = "data:";
        if (!dataUrl.StartsWith(expectedPrefix, StringComparison.Ordinal))
        {
            throw new FormatException("Not a data URL.");
        }

        var comma = dataUrl.IndexOf(',');
        if (comma < 0)
        {
            throw new FormatException("Data URL has no payload separator.");
        }

        var metadata = dataUrl[expectedPrefix.Length..comma];
        if (!metadata.EndsWith(";base64", StringComparison.OrdinalIgnoreCase))
        {
            throw new FormatException("Data URL payload is not base64.");
        }

        var contentType = metadata[..^";base64".Length];
        if (!string.Equals(contentType, JpegContentType, StringComparison.OrdinalIgnoreCase))
        {
            throw new FormatException($"Expected {JpegContentType} but got '{contentType}'.");
        }

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(dataUrl[(comma + 1)..]);
        }
        catch (FormatException ex)
        {
            throw new FormatException("Data URL payload is not valid base64.", ex);
        }

        if (bytes.Length == 0)
        {
            throw new FormatException("Data URL payload is empty.");
        }

        return new Scan
        {
            Image = bytes,
            ContentType = contentType,
            Width = width,
            Height = height,
            Source = source,
        };
    }

    /// <summary>Rebuilds a data URL for display. The bytes are never written to disk.</summary>
    public string ToDataUrl() => $"data:{ContentType};base64,{Convert.ToBase64String(Image.Span)}";
}
