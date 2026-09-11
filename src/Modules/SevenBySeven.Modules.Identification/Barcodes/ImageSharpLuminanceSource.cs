using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using ZXing;

namespace SevenBySeven.Modules.Identification.Barcodes;

/// <summary>
/// Bridges ImageSharp to ZXing. The published ZXing ImageSharp binding is pinned to
/// ImageSharp 1.x and is binary-incompatible with current versions, so this adapter
/// exists instead — it is the whole of what that package provided.
/// </summary>
internal sealed class ImageSharpLuminanceSource : LuminanceSource
{
    private readonly byte[] _luminances;

    public ImageSharpLuminanceSource(Image<Rgba32> image)
        : base(image.Width, image.Height)
    {
        _luminances = new byte[image.Width * image.Height];

        image.ProcessPixelRows(accessor =>
        {
            var index = 0;

            for (var y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);

                for (var x = 0; x < row.Length; x++)
                {
                    ref var pixel = ref row[x];

                    // ITU-R BT.601 luma, the weighting ZXing's own sources use.
                    _luminances[index++] = (byte)((pixel.R * 299 + pixel.G * 587 + pixel.B * 114) / 1000);
                }
            }
        });
    }

    private ImageSharpLuminanceSource(byte[] luminances, int width, int height)
        : base(width, height) => _luminances = luminances;

    public override byte[] Matrix => _luminances;

    public override byte[] getRow(int y, byte[]? row)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(y);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(y, Height);

        if (row is null || row.Length < Width)
        {
            row = new byte[Width];
        }

        Array.Copy(_luminances, y * Width, row, 0, Width);

        return row;
    }

    public override LuminanceSource crop(int left, int top, int width, int height)
    {
        var cropped = new byte[width * height];

        for (var y = 0; y < height; y++)
        {
            Array.Copy(_luminances, ((top + y) * Width) + left, cropped, y * width, width);
        }

        return new ImageSharpLuminanceSource(cropped, width, height);
    }

    public override bool CropSupported => true;
}
