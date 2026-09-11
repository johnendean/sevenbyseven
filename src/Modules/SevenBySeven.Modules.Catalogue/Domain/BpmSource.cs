namespace SevenBySeven.Modules.Catalogue.Domain;

/// <summary>
/// Where a Track's BPM came from. Recorded because no catalogue source supplies
/// BPM (see docs/adr/0003), so every value is either hand-entered or from a
/// third party whose terms we may need to honour.
/// </summary>
public enum BpmSource
{
    Manual = 1,
    GetSongBpm = 2,
}
