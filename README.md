# Seven by Seven

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

## Layout

```
src/SevenBySeven.AppHost          Aspire orchestration: Postgres, pgweb, the web app
src/SevenBySeven.ServiceDefaults  telemetry, health checks, HTTP resilience
src/SevenBySeven.Web              Blazor Server host; composes the modules
src/SevenBySeven.Shared           IModule, the DbContext, schema names
src/Modules/…Scanning             camera capture and photo intake
src/Modules/…Identification       barcode decode, sleeve reading, Discogs search
src/Modules/…Catalogue            Release and Track — cached Discogs data
src/Modules/…Collection           Copy — the records I own
tests/SevenBySeven.Tests          unit tests, plus Aspire integration test support
```

Modules are separate projects so the boundaries are enforced by the compiler rather than
by convention. Each contributes services and entity configuration through `IModule`, and
carries its own pages.

## Tests

```bash
dotnet test
```
