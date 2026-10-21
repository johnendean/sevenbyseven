using SevenBySeven.Modules.Identification.Contracts;

namespace SevenBySeven.Modules.Identification;

/// <summary>
/// Turns a photograph into Match Candidates. This is the module's entire public
/// surface: callers hand over bytes and get candidates back, and never see the
/// barcode, vision or catalogue machinery behind it.
/// </summary>
public interface ISleeveIdentifier
{
    /// <param name="format">
    /// Narrows the catalogue number and artist-and-title searches. A barcode names one
    /// Release exactly, so it is never narrowed.
    /// </param>
    Task<IdentificationResult> IdentifyAsync(
        ReadOnlyMemory<byte> image,
        string contentType,
        Format format = Format.Vinyl,
        CancellationToken cancellationToken = default);
}
