using SevenBySeven.Modules.Scanning.Domain;

namespace SevenBySeven.Tests.Scanning;

public class ScanTests
{
    // A one-pixel JPEG is unnecessary here: FromDataUrl validates the envelope, not the pixels.
    private const string Payload = "/9j/4AAQSkZJRg==";
    private static readonly string ValidDataUrl = $"data:image/jpeg;base64,{Payload}";

    [Fact]
    public void FromDataUrl_reads_the_payload_and_dimensions()
    {
        var scan = Scan.FromDataUrl(ValidDataUrl, 1600, 1200, CaptureSource.Camera);

        Assert.Equal(Convert.FromBase64String(Payload), scan.Image.ToArray());
        Assert.Equal("image/jpeg", scan.ContentType);
        Assert.Equal(1600, scan.Width);
        Assert.Equal(1200, scan.Height);
        Assert.Equal(CaptureSource.Camera, scan.Source);
        Assert.Equal(Convert.FromBase64String(Payload).Length, scan.SizeBytes);
    }

    [Fact]
    public void ToDataUrl_round_trips()
    {
        var scan = Scan.FromDataUrl(ValidDataUrl, 100, 100, CaptureSource.File);

        Assert.Equal(ValidDataUrl, scan.ToDataUrl());
    }

    [Theory]
    [InlineData("not a data url at all")]
    [InlineData("data:image/jpeg;base64")]                  // no separator
    [InlineData("data:image/jpeg,notbase64")]               // not declared base64
    [InlineData("data:image/png;base64,iVBORw0KGgo=")]      // wrong image type
    [InlineData("data:image/jpeg;base64,!!!not-base64!!!")]
    [InlineData("data:image/jpeg;base64,")]                 // empty payload
    public void FromDataUrl_rejects_malformed_input(string dataUrl) =>
        Assert.Throws<FormatException>(
            () => Scan.FromDataUrl(dataUrl, 100, 100, CaptureSource.Camera));

    [Theory]
    [InlineData(0, 100)]
    [InlineData(100, 0)]
    [InlineData(-1, 100)]
    public void FromDataUrl_rejects_impossible_dimensions(int width, int height) =>
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Scan.FromDataUrl(ValidDataUrl, width, height, CaptureSource.Camera));
}
