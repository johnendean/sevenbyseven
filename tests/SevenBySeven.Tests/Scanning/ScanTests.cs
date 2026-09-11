using SevenBySeven.Modules.Scanning.Domain;

namespace SevenBySeven.Tests.Scanning;

public class ScanTests
{
    // A whole JPEG is unnecessary here: FromJpeg validates the envelope, not the pixels.
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10];

    [Fact]
    public void FromJpeg_keeps_the_payload_and_dimensions()
    {
        var scan = Scan.FromJpeg(Jpeg, 1600, 1200, CaptureSource.Camera);

        Assert.Equal(Jpeg, scan.Image.ToArray());
        Assert.Equal("image/jpeg", scan.ContentType);
        Assert.Equal(1600, scan.Width);
        Assert.Equal(1200, scan.Height);
        Assert.Equal(CaptureSource.Camera, scan.Source);
        Assert.Equal(Jpeg.Length, scan.SizeBytes);
    }

    [Fact]
    public void ToDataUrl_renders_the_payload_for_display()
    {
        var scan = Scan.FromJpeg(Jpeg, 100, 100, CaptureSource.File);

        Assert.Equal($"data:image/jpeg;base64,{Convert.ToBase64String(Jpeg)}", scan.ToDataUrl());
    }

    [Theory]
    [InlineData(new byte[0])]                                   // nothing came through
    [InlineData(new byte[] { 0xFF, 0xD8 })]                     // truncated before the first marker
    [InlineData(new byte[] { 0x89, 0x50, 0x4E, 0x47 })]         // a PNG
    public void FromJpeg_rejects_anything_but_a_jpeg(byte[] bytes) =>
        Assert.Throws<FormatException>(
            () => Scan.FromJpeg(bytes, 100, 100, CaptureSource.Camera));

    [Theory]
    [InlineData(0, 100)]
    [InlineData(100, 0)]
    [InlineData(-1, 100)]
    public void FromJpeg_rejects_impossible_dimensions(int width, int height) =>
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Scan.FromJpeg(Jpeg, width, height, CaptureSource.Camera));
}
