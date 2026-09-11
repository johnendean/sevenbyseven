using SevenBySeven.Modules.Identification.Domain;

namespace SevenBySeven.Modules.Identification.Vision;

/// <summary>
/// Reads what it can off a photographed sleeve or centre label. No API turns an image
/// into a release (docs/adr/0002), so this is the extraction half of the pipeline: it
/// produces search terms, never an answer.
/// </summary>
public interface ISleeveReader
{
    Task<SleeveDetails> ReadAsync(
        ReadOnlyMemory<byte> image,
        string contentType,
        CancellationToken cancellationToken = default);
}
