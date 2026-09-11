using SevenBySeven.Modules.Scanning;
using SevenBySeven.Modules.Scanning.Domain;

namespace SevenBySeven.Tests.Scanning;

public class ScanSessionTests
{
    private static Scan AnyScan() =>
        Scan.FromDataUrl("data:image/jpeg;base64,/9j/4AAQSkZJRg==", 10, 10, CaptureSource.File);

    [Fact]
    public void A_new_session_holds_no_scan()
    {
        var session = new ScanSession();

        Assert.False(session.HasScan);
        Assert.Null(session.Current);
    }

    [Fact]
    public void Setting_a_scan_raises_Changed()
    {
        var session = new ScanSession();
        var raised = 0;
        session.Changed += () => raised++;

        session.Set(AnyScan());

        Assert.True(session.HasScan);
        Assert.Equal(1, raised);
    }

    [Fact]
    public void Clearing_discards_the_scan_and_raises_Changed()
    {
        var session = new ScanSession();
        session.Set(AnyScan());
        var raised = 0;
        session.Changed += () => raised++;

        session.Clear();

        Assert.False(session.HasScan);
        Assert.Null(session.Current);
        Assert.Equal(1, raised);
    }

    [Fact]
    public void Clearing_an_empty_session_raises_nothing()
    {
        var session = new ScanSession();
        var raised = 0;
        session.Changed += () => raised++;

        session.Clear();

        Assert.Equal(0, raised);
    }
}
