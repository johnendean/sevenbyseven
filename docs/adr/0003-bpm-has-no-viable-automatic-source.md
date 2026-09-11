# BPM has no viable automatic source

BPM is the one field this project wants that a catalogue lookup cannot supply — it is a per-track audio measurement, and the obvious sources have closed. As of September 2026, AcousticBrainz has been offline since February 2022 with only a frozen 2022 data dump remaining, and Spotify's `audio-features` endpoint was deprecated in November 2024 with access retained only by applications that already held Extended Quota Mode before that date. There is no waitlist and no announced restoration, so a new application cannot obtain it at any price. BPM is therefore an optional field on Track, populated by hand, with its source recorded alongside it.

This is written down because a future reader will otherwise reasonably suggest "just call the Spotify audio-features endpoint" and lose a day discovering why that is not possible.

## Considered Options

**GetSongBPM** remains live and free, and stays open as a later enrichment source. It requires a permanently visible link back to their site wherever their data is shown, which is a trivial cost for a personal application but is a real obligation.

**TuneBat** and **Deezer** could not be pinned down on licensing for hobby use and would need direct verification before being relied on.

**Local analysis** with Essentia or librosa is the only route with no external dependency and no runtime attribution, but it requires ripping the records first, which is a substantially larger project than cataloguing them.

## Consequences

- BPM lives on Track, which belongs to the Release, so a tempo is entered once and shared by every Copy of that pressing.
- Because BPM is hand-entered data living inside the otherwise re-fetchable Catalogue, refreshing a Release must merge rather than replace, matching Tracks on position and leaving BPM untouched. Refresh happens only on explicit request; there is no background refresh.
