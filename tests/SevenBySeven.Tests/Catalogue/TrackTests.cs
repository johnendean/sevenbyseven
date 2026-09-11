using SevenBySeven.Modules.Catalogue.Domain;

namespace SevenBySeven.Tests.Catalogue;

public class TrackTests
{
    private static Track AnyTrack() => new() { Position = "A1", Title = "So What" };

    [Fact]
    public void A_new_track_has_no_tempo()
    {
        var track = AnyTrack();

        Assert.Null(track.Bpm);
        Assert.Null(track.BpmSource);
    }

    [Fact]
    public void Setting_a_tempo_records_where_it_came_from()
    {
        var track = AnyTrack();

        track.SetBpm(118.5m, BpmSource.Manual);

        Assert.Equal(118.5m, track.Bpm);
        Assert.Equal(BpmSource.Manual, track.BpmSource);
    }

    [Fact]
    public void Clearing_a_tempo_clears_its_source_too()
    {
        var track = AnyTrack();
        track.SetBpm(118.5m, BpmSource.GetSongBpm);

        track.ClearBpm();

        Assert.Null(track.Bpm);
        Assert.Null(track.BpmSource);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void A_tempo_must_be_positive(decimal bpm) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => AnyTrack().SetBpm(bpm, BpmSource.Manual));
}
