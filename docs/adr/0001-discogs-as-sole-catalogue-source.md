# Discogs as the sole catalogue source

Identifying a record I own means identifying a *pressing*, not an album — the 1959 original and the 2014 reissue share a sleeve but are different objects on my shelf. Discogs is organised around exactly that distinction, routinely creating separate entries for differences as small as matrix and runout etchings, and it carries the genres, styles, tracklists and catalogue numbers this project needs. We therefore use Discogs as the only catalogue source, and shape the Release and Track model after theirs.

## Considered Options

**MusicBrainz** was the main alternative: CC0 data, a cleaner API, stable identifiers, and no rate limit worth worrying about. It was rejected because its release model is coarser for pure pressing variants — it groups more under a single release than Discogs does — which is precisely the granularity this project exists to capture. It remains a plausible secondary source for artist and work relationships, so the catalogue lookup sits behind an interface.

**Both, from the start** was rejected as unnecessary work for a single-user application. Reconciling two identifier schemes is a real cost and buys nothing until there is a concrete question neither source can answer alone.

## Consequences

- Discogs' API terms grant a personal, non-transferable licence. This is fine for a hobby application used by one person, but the project cannot be opened up to other users or have its catalogue data redistributed without revisiting this.
- The API is rate limited to 60 requests per minute with a personal access token. Hand-scanning will never approach that; the limiter exists to catch runaway loops, not to shape the design.
- The underlying Discogs data is CC0, so holding a local Catalogue of Releases is unproblematic.
- The shape of the Release entity follows Discogs' model. Adopting MusicBrainz later means mapping onto that shape, not replacing it.
