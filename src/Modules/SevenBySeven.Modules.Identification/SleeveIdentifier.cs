using Microsoft.Extensions.Logging;
using SevenBySeven.Modules.Identification.Barcodes;
using SevenBySeven.Modules.Identification.Discogs;
using SevenBySeven.Modules.Identification.Domain;
using SevenBySeven.Modules.Identification.Vision;

namespace SevenBySeven.Modules.Identification;

/// <summary>
/// The pipeline from docs/adr/0002: decode a barcode when there is one, otherwise read
/// the sleeve and search on whatever was legible. Each step is tried only when the one
/// before it produced nothing, so the cheapest and most exact evidence wins.
/// </summary>
internal sealed class SleeveIdentifier(
    IBarcodeScanner barcodes,
    ISleeveReader sleeves,
    IDiscogsCatalogue catalogue,
    ILogger<SleeveIdentifier> logger) : ISleeveIdentifier
{
    public async Task<IdentificationResult> IdentifyAsync(
        ReadOnlyMemory<byte> image,
        string contentType,
        Format format = Format.Vinyl,
        CancellationToken cancellationToken = default)
    {
        if (image.IsEmpty)
        {
            return IdentificationResult.Nothing();
        }

        // Any search that could not be made. Only matters when nothing is found: then it
        // decides between "Discogs had nothing" and "Discogs was not really asked".
        var searchFailed = false;

        async Task<DiscogsSearch> Search(DiscogsQuery query)
        {
            var search = await catalogue.SearchAsync(query, cancellationToken);
            searchFailed |= search.Failed;
            return search;
        }

        // 1. A barcode is exact. Free to attempt, and decisive when it works.
        if (barcodes.TryRead(image) is { } barcode)
        {
            logger.LogInformation("Read barcode {Barcode} from the sleeve.", barcode);

            var byBarcode = await Search(new DiscogsQuery { Barcode = barcode });
            if (byBarcode.Candidates.Count > 0)
            {
                var found = new SleeveDetails { Barcode = barcode };
                return new IdentificationResult(byBarcode.Candidates, found, IdentificationRoute.Barcode);
            }

            logger.LogInformation("Barcode {Barcode} matched nothing; falling back to reading the sleeve.", barcode);
        }

        // 2. Read whatever is printed and search on that.
        var details = await sleeves.ReadAsync(image, contentType, cancellationToken);

        if (details.IsEmpty)
        {
            logger.LogInformation("Nothing legible was read from the sleeve.");
            return IdentificationResult.Nothing(details) with { SearchFailed = searchFailed };
        }

        // 2a. A barcode the scanner missed but the model could read.
        if (!string.IsNullOrWhiteSpace(details.Barcode))
        {
            var byReadBarcode = await Search(new DiscogsQuery { Barcode = details.Barcode });

            if (byReadBarcode.Candidates.Count > 0)
            {
                return new IdentificationResult(byReadBarcode.Candidates, details, IdentificationRoute.Barcode);
            }
        }

        // 2b. Catalogue number plus label: the vinyl collector's identifying pair.
        if (!string.IsNullOrWhiteSpace(details.CatalogueNumber))
        {
            var byCatalogueNumber = await SearchWithinFormatAsync(
                Search,
                new DiscogsQuery { CatalogueNumber = details.CatalogueNumber, LabelName = details.LabelName },
                format,
                details,
                IdentificationRoute.CatalogueNumber);

            if (byCatalogueNumber is not null)
            {
                return byCatalogueNumber;
            }
        }

        // 2c. Artist and title. Broad, and the case where confirmation matters most.
        if (!string.IsNullOrWhiteSpace(details.ArtistName) || !string.IsNullOrWhiteSpace(details.Title))
        {
            var byName = await SearchWithinFormatAsync(
                Search,
                new DiscogsQuery { ArtistName = details.ArtistName, Title = details.Title },
                format,
                details,
                IdentificationRoute.ArtistAndTitle);

            if (byName is not null)
            {
                return byName;
            }
        }

        return IdentificationResult.Nothing(details) with { SearchFailed = searchFailed };
    }

    /// <summary>
    /// Searches within the chosen Format, then as any vinyl when that finds nothing.
    /// Discogs entries do not all record their size, so a strict Format would hide the
    /// right Release; the wider answer is marked instead, and Confirmation still decides.
    /// The fallback stays on the same evidence before moving to weaker evidence: a
    /// catalogue number matched as any vinyl is a better answer than an artist and
    /// title matched within the Format.
    /// </summary>
    private async Task<IdentificationResult?> SearchWithinFormatAsync(
        Func<DiscogsQuery, Task<DiscogsSearch>> search,
        DiscogsQuery query,
        Format format,
        SleeveDetails details,
        IdentificationRoute route)
    {
        var within = await search(query with { Format = format });
        if (within.Candidates.Count > 0)
        {
            return new IdentificationResult(within.Candidates, details, route);
        }

        // Only an answer of "nothing" says the Format was too narrow. A search that failed
        // says nothing about what Discogs holds, and widening after it would mark a
        // successful retry as outside the Format when it may not be.
        if (format is Format.Vinyl || !within.MatchedNothing)
        {
            return null;
        }

        logger.LogInformation("Nothing matched as {Format}; searching any vinyl.", format);

        var anyVinyl = await search(query with { Format = Format.Vinyl });

        return anyVinyl.Candidates.Count > 0
            ? new IdentificationResult(anyVinyl.Candidates, details, route) { IsOutsideFormat = true }
            : null;
    }
}
