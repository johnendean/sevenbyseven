using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using SevenBySeven.Modules.Identification.Barcodes;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using ZXing;
using ZXing.Common;
using ZXing.OneD;

namespace SevenBySeven.Tests.Identification;

/// <summary>
/// The barcode half of identification. These draw a real EAN-13 and decode it back
/// through the real ZXing reader: the adapter between ImageSharp and ZXing is the part
/// most likely to be subtly wrong, and only a genuine decode would catch it.
/// </summary>
public class BarcodeScannerTests
{
    private const string KindOfBlue = "5099749494220";

    [Fact]
    public void A_barcode_on_a_sleeve_is_read_off_it() =>
        Assert.Equal(KindOfBlue, Scanner().TryRead(PngOf(KindOfBlue)));

    [Fact]
    public void A_sleeve_with_no_barcode_reads_as_nothing()
    {
        // The common case: most of the records worth cataloguing predate barcodes, which
        // is the whole reason the sleeve is also read for a catalogue number.
        using var blank = new Image<Rgba32>(400, 200, Color.White.ToPixel<Rgba32>());

        Assert.Null(Scanner().TryRead(AsPng(blank)));
    }

    [Fact]
    public void An_empty_capture_reads_as_nothing() =>
        Assert.Null(Scanner().TryRead(ReadOnlyMemory<byte>.Empty));

    [Theory]
    [InlineData("this is not an image")]
    [InlineData("PNG but truncated")]
    public void Bytes_that_are_not_an_image_read_as_nothing_rather_than_throwing(string junk) =>
        // A wrong-format or truncated transfer must not take the scan page down.
        Assert.Null(Scanner().TryRead(Encoding.UTF8.GetBytes(junk)));

    [Fact]
    public void Luminance_is_one_byte_per_pixel()
    {
        using var image = new Image<Rgba32>(4, 3, Color.Black.ToPixel<Rgba32>());

        var source = new ImageSharpLuminanceSource(image);

        Assert.Equal(4, source.Width);
        Assert.Equal(3, source.Height);
        Assert.Equal(12, source.Matrix.Length);
        Assert.All(source.Matrix, value => Assert.Equal(0, value));
    }

    [Fact]
    public void White_is_bright_and_black_is_dark()
    {
        using var image = new Image<Rgba32>(2, 1, Color.White.ToPixel<Rgba32>());
        image[1, 0] = Color.Black.ToPixel<Rgba32>();

        var row = new ImageSharpLuminanceSource(image).getRow(0, null);

        Assert.Equal(255, row[0]);
        Assert.Equal(0, row[1]);
    }

    [Fact]
    public void A_row_buffer_too_small_to_reuse_is_replaced()
    {
        using var image = new Image<Rgba32>(8, 2, Color.White.ToPixel<Rgba32>());
        var source = new ImageSharpLuminanceSource(image);

        Assert.Equal(8, source.getRow(0, new byte[3]).Length);
        Assert.Equal(8, source.getRow(1, new byte[8]).Length);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(2)]
    public void A_row_outside_the_image_is_a_mistake_worth_shouting_about(int y)
    {
        using var image = new Image<Rgba32>(4, 2, Color.White.ToPixel<Rgba32>());
        var source = new ImageSharpLuminanceSource(image);

        Assert.Throws<ArgumentOutOfRangeException>(() => source.getRow(y, null));
    }

    [Fact]
    public void Cropping_keeps_the_pixels_that_were_inside_it()
    {
        // ZXing crops while hunting for the barcode, so the arithmetic here decides
        // whether a barcode near an edge is found at all.
        using var image = new Image<Rgba32>(4, 4, Color.White.ToPixel<Rgba32>());
        image[3, 3] = Color.Black.ToPixel<Rgba32>();

        var cropped = new ImageSharpLuminanceSource(image).crop(2, 2, 2, 2);

        Assert.True(cropped.CropSupported);
        Assert.Equal(2, cropped.Width);
        Assert.Equal(2, cropped.Height);
        Assert.Equal(new byte[] { 255, 255, 255, 0 }, cropped.Matrix);
    }

    private static ZXingBarcodeScanner Scanner() =>
        new(NullLogger<ZXingBarcodeScanner>.Instance);

    /// <summary>Draws a real EAN-13 rather than carrying a binary fixture in the repo.</summary>
    private static ReadOnlyMemory<byte> PngOf(string barcode)
    {
        var matrix = new EAN13Writer().encode(
            barcode,
            BarcodeFormat.EAN_13,
            380,
            160,
            new EncodingOptions { Margin = 20, PureBarcode = true }.Hints);

        using var image = new Image<Rgba32>(matrix.Width, matrix.Height);

        for (var y = 0; y < matrix.Height; y++)
        {
            for (var x = 0; x < matrix.Width; x++)
            {
                image[x, y] = matrix[x, y] ? Color.Black.ToPixel<Rgba32>() : Color.White.ToPixel<Rgba32>();
            }
        }

        return AsPng(image);
    }

    private static ReadOnlyMemory<byte> AsPng(Image<Rgba32> image)
    {
        using var buffer = new MemoryStream();
        image.Save(buffer, new PngEncoder());

        return buffer.ToArray();
    }
}
