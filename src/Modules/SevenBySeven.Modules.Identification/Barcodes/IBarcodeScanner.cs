namespace SevenBySeven.Modules.Identification.Barcodes;

/// <summary>
/// Reads a retail barcode off a photographed sleeve. When one is present this is the
/// exact path — but records pressed before roughly 1982 carry no barcode at all, which
/// is why it is only the first attempt and never the only one.
/// </summary>
public interface IBarcodeScanner
{
    /// <summary>The barcode digits, or null when none could be read.</summary>
    string? TryRead(ReadOnlyMemory<byte> image);
}
