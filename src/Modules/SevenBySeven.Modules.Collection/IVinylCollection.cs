using SevenBySeven.Modules.Collection.Domain;

namespace SevenBySeven.Modules.Collection;

/// <summary>
/// The Collection: every Copy I own, and there is exactly one of it. Adding a Copy is
/// what a Confirmation amounts to — the Release it is a copy of must already be held
/// in the Catalogue, which is what makes the Copy meaningful.
/// </summary>
public interface IVinylCollection
{
    /// <summary>Adds a Copy of a Release already held in the Catalogue.</summary>
    Task<Copy> AddAsync(Guid releaseId, CopyDetails details, CancellationToken cancellationToken = default);

    /// <summary>Every Copy, most recently added first, each with the pressing it is a copy of.</summary>
    Task<IReadOnlyList<Copy>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>One Copy with its pressing and that pressing's tracks, or null if it is not mine.</summary>
    Task<Copy?> FindAsync(Guid copyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a Copy. The Release stays in the Catalogue: it is cached fact, and I may
    /// well buy another. False when there was no such Copy to remove.
    /// </summary>
    Task<bool> RemoveAsync(Guid copyId, CancellationToken cancellationToken = default);
}
