using SevenBySeven.Modules.Identification.Domain;

namespace SevenBySeven.Modules.Identification;

/// <summary>
/// Turns a photograph into Match Candidates. This is the module's entire public
/// surface: callers hand over bytes and get candidates back, and never see the
/// barcode, vision or catalogue machinery behind it.
/// </summary>
public interface ISleeveIdentifier
{
    Task<IdentificationResult> IdentifyAsync(
        ReadOnlyMemory<byte> image,
        string contentType,
        CancellationToken cancellationToken = default);
}
