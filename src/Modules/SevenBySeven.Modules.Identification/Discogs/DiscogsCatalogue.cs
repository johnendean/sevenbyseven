using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SevenBySeven.Modules.Identification.Domain;

namespace SevenBySeven.Modules.Identification.Discogs;

internal sealed class DiscogsCatalogue(
    HttpClient http,
    IOptions<DiscogsOptions> options,
    ILogger<DiscogsCatalogue> logger) : IDiscogsCatalogue
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly DiscogsOptions _options = options.Value;

    public async Task<IReadOnlyList<MatchCandidate>> SearchAsync(
        DiscogsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.IsEmpty)
        {
            return [];
        }

        if (!_options.IsConfigured)
        {
            logger.LogWarning("No Discogs token is configured, so no search was attempted.");
            return [];
        }

        var uri = BuildSearchUri(query, _options.CandidatesPerSearch);

        HttpResponseMessage response;
        try
        {
            response = await http.GetAsync(uri, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "The Discogs search request failed.");
            return [];
        }

        using (response)
        {
            if (response.StatusCode is HttpStatusCode.TooManyRequests)
            {
                // The resilience handler already retried; arriving here means Discogs is
                // still refusing, so report nothing rather than block the user.
                logger.LogWarning("Discogs is rate limiting us despite the local limiter.");
                return [];
            }

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Discogs search returned {Status}.", (int)response.StatusCode);
                return [];
            }

            var payload = await response.Content.ReadFromJsonAsync<DiscogsSearchResponse>(Json, cancellationToken);

            return payload?.Results is { Count: > 0 } results
                ? [.. results.Select(ToCandidate)]
                : [];
        }
    }

    internal static string BuildSearchUri(DiscogsQuery query, int perPage)
    {
        var parameters = new List<KeyValuePair<string, string>>
        {
            new("type", "release"),
            // Vinyl only: this catalogues records, and the CD pressing is never the answer.
            new("format", "Vinyl"),
            new("per_page", perPage.ToString()),
        };

        void Add(string name, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                parameters.Add(new KeyValuePair<string, string>(name, value.Trim()));
            }
        }

        Add("barcode", query.Barcode);
        Add("catno", query.CatalogueNumber);
        Add("label", query.LabelName);
        Add("artist", query.ArtistName);
        Add("release_title", query.Title);

        if (query.Year is { } year)
        {
            Add("year", year.ToString());
        }

        var encoded = string.Join('&', parameters.Select(p =>
            $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));

        return $"database/search?{encoded}";
    }

    private static MatchCandidate ToCandidate(DiscogsSearchResult result)
    {
        var (artist, title) = SplitTitle(result.Title);

        return new MatchCandidate(
            DiscogsReleaseId: result.Id,
            Title: title,
            ArtistName: artist,
            LabelName: result.Labels?.FirstOrDefault(),
            CatalogueNumber: result.CatalogueNumber,
            Country: result.Country,
            Year: ParseYear(result.Year),
            FormatDescription: result.Formats is { Count: > 0 } f ? string.Join(", ", f) : null,
            ThumbnailUrl: string.IsNullOrWhiteSpace(result.Thumb) ? null : result.Thumb,
            DiscogsMasterId: result.MasterId);
    }

    /// <summary>
    /// Discogs packs artist and title into one string separated by " - ". Titles contain
    /// hyphens too, so only the first separator counts.
    /// </summary>
    internal static (string Artist, string Title) SplitTitle(string? combined)
    {
        if (string.IsNullOrWhiteSpace(combined))
        {
            return (string.Empty, string.Empty);
        }

        var separator = combined.IndexOf(" - ", StringComparison.Ordinal);

        return separator < 0
            ? (string.Empty, combined.Trim())
            : (combined[..separator].Trim(), combined[(separator + 3)..].Trim());
    }

    /// <summary>Discogs years arrive as strings and are sometimes blank or partial.</summary>
    internal static int? ParseYear(string? year) =>
        int.TryParse(year, out var parsed) && parsed > 1850 && parsed < 2200 ? parsed : null;
}
