namespace SevenBySeven.Modules.Identification.Discogs;

public sealed class DiscogsOptions
{
    public const string SectionName = "Discogs";

    /// <summary>
    /// A personal access token from discogs.com/settings/developers. Full OAuth is only
    /// needed to act on another user's behalf, which this application never does.
    /// Set it with user-secrets; never in appsettings.
    /// </summary>
    public string? PersonalAccessToken { get; set; }

    public Uri BaseAddress { get; set; } = new("https://api.discogs.com/");

    /// <summary>Discogs requires an identifying user agent and rejects generic ones.</summary>
    public string UserAgent { get; set; } = "SevenBySeven/0.1 (+https://github.com/johnendean/sevenbyseven)";

    /// <summary>
    /// Discogs allows 60 authenticated requests a minute. Sitting under it means a
    /// runaway loop trips our limiter rather than theirs.
    /// </summary>
    public int RequestsPerMinute { get; set; } = 55;

    public int CandidatesPerSearch { get; set; } = 5;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(PersonalAccessToken);
}
