# Seven by Seven

[![CI](https://github.com/johnendean/sevenbyseven/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/johnendean/sevenbyseven/actions/workflows/ci.yml?query=branch%3Amain)
[![Coverage](https://coveralls.io/repos/github/johnendean/sevenbyseven/badge.svg?branch=main)](https://coveralls.io/github/johnendean/sevenbyseven?branch=main)

A catalogue of the vinyl records I own. Photograph a sleeve or centre label, and the
record is identified against Discogs and added to the collection once you confirm which
pressing it is.

The domain vocabulary lives in [CONTEXT.md](./CONTEXT.md); the decisions that shaped it
are in [docs/adr](./docs/adr).

## Prerequisites

- .NET 10 SDK
- A container runtime (Docker Desktop or Podman) — Aspire runs Postgres in a container
- A Discogs personal access token, from <https://www.discogs.com/settings/developers>
- An Anthropic API key, for reading text off a photographed sleeve

## Configuration

Both credentials are read from user secrets on the web project. Neither belongs in
`appsettings.json`.

```bash
dotnet user-secrets set "Discogs:PersonalAccessToken" "<token>" --project src/SevenBySeven.Web
dotnet user-secrets set "Identification:AnthropicApiKey" "<key>" --project src/SevenBySeven.Web
```

Without them the app still runs: identification degrades rather than failing. With no
Anthropic key only the barcode path can identify a record; with no Discogs token no
search happens at all.

## The Six Labors licence

ImageSharp decodes captured photographs for barcode reading, and Six Labors moved to a
licence-key model at version 3. Their Community licence is free for individuals and hobby
projects, which this is; register for one at <https://sixlabors.com/pricing/>.

The key is a credential, so it is not in the repository. Put your own `sixlabors.lic` at
the repository root and `Directory.Build.props` points every project at it — ImageSharp
only looks alongside the project being built, so the root copy needs that nudge.
Alternatively set `$(SixLaborsLicenseKey)` to the contents of the file, which takes
precedence; that is how CI supplies it, from the `SIXLABORS_LICENSE_KEY` secret.

Without a key, Debug builds warn and Release builds fail outright.

## Running

```bash
dotnet run --project src/SevenBySeven.AppHost
```

Aspire starts Postgres, applies migrations on boot, and serves the app at
<https://localhost:7066>. The dashboard URL is printed on startup.

## Using the camera

Browsers only grant camera access in a secure context, so the live preview works over
`localhost` or trusted HTTPS and **not** over plain HTTP from a phone on your LAN. The
page falls back to a file input with `capture="environment"`, which opens the phone's own
camera app and works anywhere — that is the path to use from your shelves.

Serving the app over trusted HTTPS on a hostname lifts that restriction, and is also the
groundwork for installing this on a phone as an app rather than a bookmark.
[docs/mobile-distribution.md](./docs/mobile-distribution.md) weighs up how to get there,
and what distributing outside the app stores does and does not allow.

## Scanning a stack

A Stack captures hands-free, for when you have a pile to get through rather than one
record. Hold a record up to the camera; it is captured on its own once it is steady, and
the camera then waits for the next one. Take one away before presenting another — the gap
is what tells the app that a different record has arrived, not the same one still in
shot.

Nothing is identified while you are capturing. When the pile is done you work through the
results at a desk: every Scan is listed with its Match Candidates, confirmed in any order,
and anything that matched nothing is abandoned from the list. Where you bought them and
where they are kept are asked once for the whole Stack rather than ten times.

A Stack holds ten records by default and stops there. Raise it for an afternoon of
cataloguing and put it back afterwards:

```bash
Scanning__MaxStackSize=20 dotnet run --project src/SevenBySeven.AppHost
```

The value is clamped — photographs are held in memory until they are confirmed or
abandoned, so an unchecked one is measured in tens of megabytes per user. The detector's
own thresholds sit under `Scanning__Detection__*` and are worth touching only if the
highlight misbehaves in your room; what they mean, and why detection is a browser
heuristic rather than a call to a model, is in
[ADR 0005](./docs/adr/0005-record-detection-is-a-browser-heuristic.md).

A Stack lives in the browser's connection to the server. It survives moving around the
app — a marker in the header says one is in progress — but closing the tab ends it, and
the photographs go with it, exactly as a single Scan does.

Since it needs the live camera, a Stack is unavailable in the plain-HTTP case above.
Scan records one at a time from your phone.

## Adding a record

Confirming a match ends the Scan and hands the pressing over to the collection. Its full
detail — tracklist included — is fetched from Discogs once and cached as a Release, and
your Copy is recorded against it. Condition, price, where you bought it and where you keep
it are all optional. Two copies of the same pressing are two records, so adding the same
release twice is the right way to say you own two.

A Stack adds Copies without stopping to ask, so those arrive with only the pressing
against them. Condition and price are the facts nothing can look up, so a record's own
page can edit them back in afterwards.

## Layout

```
src/SevenBySeven.AppHost          Aspire orchestration: Postgres, pgweb, the web app
src/SevenBySeven.ServiceDefaults  telemetry, health checks, HTTP resilience
src/SevenBySeven.Web              Blazor Server host; composes the modules
src/SevenBySeven.Shared           IModule, the DbContext, schema names
src/Modules/…Scanning             camera capture and photo intake, one record or a Stack
src/Modules/…Identification       barcode decode, sleeve reading, Discogs search
src/Modules/…Catalogue            Release and Track — cached Discogs data
src/Modules/…Collection           Copy — the records I own, and the pages for browsing them
tests/SevenBySeven.Tests          unit tests, plus Aspire integration test support
scripts                           the coverage gate CI and you both run
```

Modules are separate projects so the boundaries are enforced by the compiler rather than
by convention. Each contributes services and entity configuration through `IModule`, and
carries its own pages.

## Tests

```bash
dotnet test
```

The tests that touch the database run against SQLite in memory rather than Postgres, so
they enforce real keys, foreign keys and unique indexes without needing a container.

Coverage is collected on every run. A branch below 80% line coverage does not merge:

```bash
dotnet test --settings coverlet.runsettings --collect "XPlat Code Coverage" \
  --results-directory TestResults
./scripts/check-coverage.sh
```

CI runs exactly that, so a red gate is reproducible before you push. Pass a different bar
with `COVERAGE_THRESHOLD=85 ./scripts/check-coverage.sh` when you want to see how far a
branch is from one.

`coverlet.runsettings` decides what the figure is measured over. Out of it are the Aspire
AppHost, the web host and ServiceDefaults, which are orchestration and composition no unit
test reaches; the EF migrations and model snapshot, which are generated and outnumber the
code by some margin; and the Razor components, whose counted lines are the generated
render tree rather than behaviour — see [ADR 0004](./docs/adr/0004-coverage-is-measured-without-razor-components.md).
Logic worth testing is lifted out of a page into its module instead, as `Scan.FromJpeg` is.

Coveralls draws the badge above and comments on a pull request with the change the branch
would make. It reports rather than gates, so an outage at their end leaves the run green.
