using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using ZXing;
using ZXing.Common;

namespace SevenBySeven.Modules.Identification.Barcodes;

internal sealed class ZXingBarcodeScanner(ILogger<ZXingBarcodeScanner> logger) : IBarcodeScanner
{
    private static readonly BarcodeFormat[] RetailFormats =
    [
        BarcodeFormat.EAN_13,
        BarcodeFormat.UPC_A,
        BarcodeFormat.EAN_8,
        BarcodeFormat.UPC_E,
    ];

    public string? TryRead(ReadOnlyMemory<byte> image)
    {
        if (image.IsEmpty)
        {
            return null;
        }

        try
        {
            using var loaded = Image.Load<Rgba32>(image.Span);

            var reader = new BarcodeReaderGeneric
            {
                AutoRotate = true,
                Options = new DecodingOptions
                {
                    // A barcode photographed at an angle on a glossy sleeve needs the
                    // slower, more thorough pass to stand any chance.
                    TryHarder = true,
                    PossibleFormats = RetailFormats,
                },
            };

            var result = reader.Decode(new ImageSharpLuminanceSource(loaded));

            return string.IsNullOrWhiteSpace(result?.Text) ? null : result.Text.Trim();
        }
        catch (Exception ex) when (ex is UnknownImageFormatException or InvalidImageContentException)
        {
            logger.LogWarning(ex, "The captured image could not be decoded for barcode reading.");
            return null;
        }
    }
}
